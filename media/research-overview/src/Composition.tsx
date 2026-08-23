import React from "react";
import {AbsoluteFill, Composition, Easing, interpolate, Sequence, spring, useCurrentFrame, useVideoConfig} from "remotion";

type SceneProps = {kicker: string; title: React.ReactNode; index: string; children?: React.ReactNode};

const Scene: React.FC<SceneProps> = ({kicker, title, index, children}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const enter = spring({frame, fps, config: {damping: 18, stiffness: 95}});
  const opacity = interpolate(frame, [0, 10], [0, 1], {extrapolateRight: "clamp"});
  return <AbsoluteFill className="scene" style={{opacity}}>
    <div className="scene-number">{index}</div>
    <div className="scene-copy" style={{transform: `translateY(${(1-enter)*55}px)`}}>
      <div className="kicker">{kicker}</div>
      <h1>{title}</h1>
      {children}
    </div>
    <div className="scene-footer"><span>CDD RESEARCH PROGRAM</span><span>SPOT · EVIDENCE · PROMOTION</span></div>
  </AbsoluteFill>;
};

const Flow: React.FC = () => {
  const frame = useCurrentFrame();
  const steps = ["Intent", "Candidate", "Assurance", "Evidence", "Promotion", "Outcome"];
  return <div className="flow">{steps.map((step, i) => {
    const visible = interpolate(frame, [18+i*8, 26+i*8], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.out(Easing.quad)});
    return <React.Fragment key={step}><div className={`flow-step f${i}`} style={{opacity: visible, transform: `translateY(${(1-visible)*22}px)`}}><small>0{i+1}</small><b>{step}</b></div>{i < steps.length-1 && <span className="arrow" style={{opacity: visible}}>→</span>}</React.Fragment>;
  })}</div>;
};

const Claims: React.FC = () => {
  const frame = useCurrentFrame();
  const rows = [
    ["CLAIM", "Autonomie braucht unabhängige, risikoadaptive Gates", "PROPOSED"],
    ["EVIDENZ", "Reproduzierbare Tests, Provenienz und Replay", "MEASURED"],
    ["GRENZE", "Die Invariante und Promotion bleiben verantwortete Setzungen", "OPEN"],
  ];
  return <div className="claim-rows">{rows.map((row, i) => {
    const p = spring({frame: frame-i*10, fps: 30, config: {damping: 20}});
    return <div className="claim-row" key={row[0]} style={{opacity: p, transform: `translateX(${(1-p)*90}px)`}}><span>{row[0]}</span><b>{row[1]}</b><em>{row[2]}</em></div>;
  })}</div>;
};

const ProjectGrid: React.FC = () => {
  const frame = useCurrentFrame();
  const projects = ["CDD Core", "CDD Studio", "EIDOS", "CourseForge", "Research Track", "Research Studio"];
  return <div className="project-grid">{projects.map((name, i) => {
    const p = spring({frame: frame-i*5, fps: 30, config: {damping: 18, stiffness: 110}});
    return <div className="project-card" key={name} style={{opacity:p, transform:`scale(${.88+.12*p})`}}><small>0{i+1}</small><b>{name}</b></div>;
  })}</div>;
};

const Closing: React.FC = () => {
  const frame = useCurrentFrame();
  const line = interpolate(frame, [5, 60], [0, 100], {extrapolateRight:"clamp", easing:Easing.inOut(Easing.cubic)});
  return <Scene kicker="DEINE ROLLE" index="05" title={<>Prüfe die Prämisse.<br/>Fordere den Beleg.<br/><span>Setze die Grenze.</span></>}>
    <div className="closing-line" style={{width:`${line}%`}}/>
    <p className="url">koschnag.github.io/cong-driven-development/research/</p>
  </Scene>;
};

export const ResearchOverview: React.FC = () => <AbsoluteFill>
  <Sequence durationInFrames={180}><Scene kicker="CONG-DRIVEN DEVELOPMENT" index="01" title={<>Softwareevolution,<br/><span>die sich belegen muss.</span></>}><p className="subtitle">Ein öffentliches Forschungsprogramm für evidence-gated software evolution.</p></Scene></Sequence>
  <Sequence from={180} durationInFrames={180}><Scene kicker="DAS KERNMODELL" index="02" title={<>Nicht „Agent sagt fertig“.<br/><span>Evidence entscheidet.</span></>}><Flow/></Scene></Sequence>
  <Sequence from={360} durationInFrames={180}><Scene kicker="EPISTEMISCHE DISZIPLIN" index="03" title={<>Claims, Evidenz<br/>und Grenzen <span>bleiben getrennt.</span></>}><Claims/></Scene></Sequence>
  <Sequence from={540} durationInFrames={180}><Scene kicker="EIN SPOT · MEHRERE PROJEKTIONEN" index="04" title={<>Vom Kernel bis<br/><span>zum Forschungsbriefing.</span></>}><ProjectGrid/></Scene></Sequence>
  <Sequence from={720} durationInFrames={180}><Closing/></Sequence>
</AbsoluteFill>;

export const MyComposition: React.FC = () => <Composition id="ResearchOverview" component={ResearchOverview} durationInFrames={900} fps={30} width={1920} height={1080}/>;
