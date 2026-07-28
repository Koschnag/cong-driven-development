(() => {
  const slides = [...document.querySelectorAll(".slide")];
  let index = 0;
  const show = (next) => {
    index = Math.max(0, Math.min(slides.length - 1, next));
    slides.forEach((slide, i) => slide.classList.toggle("active", i === index));
    document.body.classList.toggle(
      "dark-header",
      slides[index].classList.contains("dark"),
    );
    document.querySelector("#counter").textContent =
      `${String(index + 1).padStart(2, "0")} / ${String(slides.length).padStart(2, "0")}`;
    document.querySelector("#progress i").style.width =
      `${((index + 1) / slides.length) * 100}%`;
  };
  document
    .querySelector("#prev")
    .addEventListener("click", () => show(index - 1));
  document
    .querySelector("#next")
    .addEventListener("click", () => show(index + 1));
  document
    .querySelector("#fullscreen")
    .addEventListener("click", () =>
      document.fullscreenElement
        ? document.exitFullscreen()
        : document.documentElement.requestFullscreen(),
    );
  addEventListener("keydown", (event) => {
    if (["ArrowRight", "PageDown", " "].includes(event.key)) show(index + 1);
    if (["ArrowLeft", "PageUp"].includes(event.key)) show(index - 1);
  });
  fetch("../ide/_demo/spot.json")
    .then((response) => response.json())
    .then((entries) => {
      const count = (kind) =>
        entries.filter((entry) => entry?.Payload?.Case === kind).length;
      document.querySelector("[data-nodes]").textContent = entries.length;
      document.querySelector("[data-claims]").textContent =
        count("ResearchClaimNode");
      document.querySelector("[data-sources]").textContent =
        count("KnowledgeNode");
    })
    .catch(() => {});
  show(0);
})();
