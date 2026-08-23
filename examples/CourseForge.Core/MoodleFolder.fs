namespace CourseForge.Core

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Xml
open System.Xml.Linq

/// Datensparsamer Adapter für einen bereits extrahierten Moodle-Backup-Ordner.
/// Er liest nur Kurs- und Abschnittsmetadaten; Nutzer-, Bewertungs- und Inhaltsdateien
/// werden weder deserialisiert noch in das öffentliche Modell übernommen.
module MoodleFolder =

    let private elementValue name (document: XDocument) =
        document.Descendants()
        |> Seq.tryFind (fun element -> element.Name.LocalName = name)
        |> Option.map (fun element -> element.Value.Trim())
        |> Option.filter (String.IsNullOrWhiteSpace >> not)

    let private maxMetadataBytes = 5L * 1024L * 1024L

    let private loadXml (path: string) =
        try
            if FileInfo(path).Length > maxMetadataBytes then
                Error(InvalidMetadata(Path.GetFileName path, "metadata file exceeds 5 MiB"))
            else
                let settings =
                    XmlReaderSettings(
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                        MaxCharactersInDocument = maxMetadataBytes)
                use stream = File.OpenRead path
                use reader = XmlReader.Create(stream, settings)
                Ok(XDocument.Load(reader, LoadOptions.None))
        with ex ->
            Error(InvalidMetadata(Path.GetFileName path, ex.Message))

    let private hasAttribute attribute attributes =
        (attributes &&& attribute) = attribute

    /// Rekursive Auflistung ohne Links zu folgen. SearchOption.AllDirectories wäre
    /// hier zu permissiv, weil verlinkte Verzeichnisse aus dem Importroot führen können.
    let rec private collectFiles (directory: string) =
        Directory.EnumerateFileSystemEntries directory
        |> Seq.fold (fun (files, linked) entry ->
            let attributes = File.GetAttributes entry
            if hasAttribute FileAttributes.ReparsePoint attributes then
                files, true
            elif hasAttribute FileAttributes.Directory attributes then
                let nested, nestedLinked = collectFiles entry
                nested @ files, linked || nestedLinked
            else
                FileInfo entry :: files, linked) ([], false)

    let private metadataFiles (files: FileInfo list) =
        files
        |> List.filter (fun file ->
            file.Name = "moodle_backup.xml"
            || (file.Name = "section.xml"
                && file.DirectoryName <> null
                && Path.GetFileName(file.DirectoryName).StartsWith("section_", StringComparison.Ordinal)))

    let private fingerprint (root: string) (files: FileInfo list) =
        let normalizedRoot = Path.GetFullPath root
        let material =
            files
            |> List.sortBy (fun file -> file.FullName)
            |> List.collect (fun file ->
                [ Path.GetRelativePath(normalizedRoot, file.FullName).Replace('\\', '/')
                  File.ReadAllText file.FullName ])
            |> String.concat "\n"
            |> Encoding.UTF8.GetBytes
        use sha = SHA256.Create()
        sha.ComputeHash material
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    let private sectionFromFile position (file: FileInfo) =
        match loadXml file.FullName with
        | Error error -> Error error
        | Ok document ->
            let fallbackId = Path.GetFileName(file.DirectoryName).Replace("section_", "")
            let id = elementValue "id" document |> Option.defaultValue fallbackId
            let title = elementValue "name" document
            Ok(
                { Id = id
                  Title = title |> Option.defaultValue (sprintf "Section %d" position)
                  Position = position },
                if title.IsNone then Some(SectionWithoutName id) else None)

    /// Liest nur das minimale Course-IR. Das Entpacken von .mbz-Archiven gehört in
    /// einen separaten, quota-limitierten Sandbox-Adapter und ist absichtlich nicht hier.
    let importExtractedFolder (limits: ImportLimits) (root: string) : Result<CourseImport, ImportError list> =
        if String.IsNullOrWhiteSpace root || not (Directory.Exists root) then
            Error [ FolderNotFound ]
        else
            let files, linkedEntry =
                if hasAttribute FileAttributes.ReparsePoint (DirectoryInfo(root).Attributes) then
                    [], true
                else
                    collectFiles root
            let totalBytes = files |> List.sumBy (fun file -> file.Length)
            let errors =
                [ if files.Length > limits.MaxFiles then
                      FileLimitExceeded(files.Length, limits.MaxFiles)
                  if totalBytes > limits.MaxTotalBytes then
                      SizeLimitExceeded(totalBytes, limits.MaxTotalBytes)
                  if linkedEntry then
                      LinkedFileRejected ]
            if not errors.IsEmpty then
                Error errors
            else
                let manifest =
                    files
                    |> List.tryFind (fun file ->
                        file.Name = "moodle_backup.xml"
                        && String.Equals(
                            Path.GetDirectoryName(file.FullName),
                            Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar),
                            StringComparison.OrdinalIgnoreCase))
                match manifest with
                | None -> Error [ MoodleManifestMissing ]
                | Some manifestFile ->
                    match loadXml manifestFile.FullName with
                    | Error error -> Error [ error ]
                    | Ok document ->
                        let title = elementValue "original_course_fullname" document |> Option.defaultValue "Untitled course"
                        let shortName = elementValue "original_course_shortname" document |> Option.defaultValue "course"
                        let courseId = elementValue "original_course_id" document |> Option.defaultValue shortName
                        let sectionResults =
                            files
                            |> List.filter (fun file ->
                                file.Name = "section.xml"
                                && file.DirectoryName <> null
                                && Path.GetFileName(file.DirectoryName).StartsWith("section_", StringComparison.Ordinal))
                            |> List.sortBy (fun file -> file.FullName)
                            |> List.mapi (fun index file -> sectionFromFile (index + 1) file)
                        let sectionErrors =
                            sectionResults |> List.choose (function Error error -> Some error | Ok _ -> None)
                        if not sectionErrors.IsEmpty then
                            Error sectionErrors
                        else
                            let sections, sectionFindings =
                                sectionResults
                                |> List.choose (function Ok result -> Some result | Error _ -> None)
                                |> List.unzip
                            let meta = metadataFiles files
                            let ignored = files.Length - meta.Length
                            let sensitiveNames =
                                files
                                |> List.exists (fun file ->
                                    let name = file.Name.ToLowerInvariant()
                                    name = "users.xml" || name.Contains("grade") || name.Contains("log"))
                            let findings =
                                [ if sensitiveNames then SensitiveDataExcluded
                                  if ignored > 0 then NonMetadataFilesIgnored ignored
                                  yield! sectionFindings |> List.choose id ]
                            Ok
                                { Course =
                                    { Id = courseId
                                      Title = title
                                      ShortName = shortName
                                      Sections = sections }
                                  SourceFingerprint = fingerprint root meta
                                  Findings = findings }
