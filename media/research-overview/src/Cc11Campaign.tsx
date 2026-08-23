import React from "react";
import {
  AbsoluteFill,
  Composition,
  Easing,
  interpolate,
  Sequence,
  useCurrentFrame,
} from "remotion";

const easeOut = Easing.bezier(0.16, 1, 0.3, 1);
const easeInOut = Easing.bezier(0.45, 0, 0.55, 1);

type FrameProps = {
  children: React.ReactNode;
  index: string;
  label: string;
  tone?: "paper" | "ink" | "signal" | "blue";
  vertical?: boolean;
};

const EditorialFrame: React.FC<FrameProps> = ({
  children,
  index,
  label,
  tone = "paper",
  vertical = false,
}) => {
  const frame = useCurrentFrame();
  return (
    <AbsoluteFill className={`cc11-frame tone-${tone} ${vertical ? "is-vertical" : ""}`}>
      <div
        className="cc11-rule rule-top"
        style={{
          scale: `${interpolate(frame, [0, 42], [0, 1], {
            easing: easeOut,
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
          })} 1`,
        }}
      />
      <div className="cc11-frame-meta">
        <span>CC11 / RESEARCH RELEASE</span>
        <span>{label}</span>
        <span>{index}</span>
      </div>
      <div className="cc11-frame-content">{children}</div>
      <div className="cc11-frame-footer">
        <span>PRIVATE MEMORY</span>
        <i />
        <span>TYPED ACTION</span>
        <i />
        <span>EVIDENCE BEFORE AUTHORITY</span>
      </div>
    </AbsoluteFill>
  );
};

const Opening: React.FC<{vertical?: boolean}> = ({vertical = false}) => {
  const frame = useCurrentFrame();
  return (
    <EditorialFrame index="01" label="THE QUESTION" vertical={vertical}>
      <div className="cc11-opening">
        <p
          className="cc11-kicker"
          style={{
            opacity: interpolate(frame, [10, 36], [0, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
          }}
        >
          Generation is cheap.
        </p>
        <h1>
          <span
            style={{
              opacity: interpolate(frame, [22, 58], [0, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
              translate: `0 ${interpolate(frame, [22, 58], [70, 0], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              })}px`,
            }}
          >
            AI can generate
          </span>
          <em
            style={{
              opacity: interpolate(frame, [52, 88], [0, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
              translate: `0 ${interpolate(frame, [52, 88], [70, 0], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              })}px`,
            }}
          >
            an answer.
          </em>
        </h1>
        <div
          className="cc11-counter"
          style={{
            opacity: interpolate(frame, [78, 108], [0, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
          }}
        >
          <span>But can it prove what the answer is allowed to become?</span>
        </div>
      </div>
    </EditorialFrame>
  );
};

const Boundary: React.FC<{vertical?: boolean}> = ({vertical = false}) => {
  const frame = useCurrentFrame();
  const rows = [
    ["SOURCE", "preserved"],
    ["CLAIM", "typed"],
    ["EXPERIMENT", "labelled"],
    ["EVIDENCE", "bound"],
    ["AUTHORITY", "explicit"],
  ];

  return (
    <EditorialFrame index="02" label="THE BOUNDARY" tone="ink" vertical={vertical}>
      <div className="cc11-boundary">
        <h2>
          <span
            style={{
              opacity: interpolate(frame, [12, 46], [0, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
            }}
          >
            CC11 keeps
          </span>
          <em
            style={{
              opacity: interpolate(frame, [38, 72], [0, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
            }}
          >
            the boundary visible.
          </em>
        </h2>
        <div className="cc11-boundary-rows">
          {rows.map(([left, right], index) => (
            <div
              key={left}
              style={{
                opacity: interpolate(frame, [58 + index * 12, 82 + index * 12], [0, 1], {
                  easing: easeOut,
                  extrapolateLeft: "clamp",
                  extrapolateRight: "clamp",
                }),
                translate: `${interpolate(frame, [58 + index * 12, 82 + index * 12], [-60, 0], {
                  easing: easeOut,
                  extrapolateLeft: "clamp",
                  extrapolateRight: "clamp",
                })}px 0`,
              }}
            >
              <span>{left}</span>
              <i />
              <strong>{right}</strong>
            </div>
          ))}
        </div>
      </div>
    </EditorialFrame>
  );
};

const Flow: React.FC<{vertical?: boolean}> = ({vertical = false}) => {
  const frame = useCurrentFrame();
  const steps = ["Source", "Claim", "Candidate", "Assurance", "Promotion", "Outcome"];
  return (
    <EditorialFrame index="03" label="THE EVIDENCE PATH" tone="blue" vertical={vertical}>
      <div className="cc11-flow-scene">
        <h2>
          <span>Not “agent says done.”</span>
          <em>Evidence travels with the change.</em>
        </h2>
        <div className="cc11-flow">
          {steps.map((step, index) => (
            <div
              className="cc11-flow-step"
              key={step}
              style={{
                opacity: interpolate(frame, [32 + index * 14, 58 + index * 14], [0, 1], {
                  easing: easeOut,
                  extrapolateLeft: "clamp",
                  extrapolateRight: "clamp",
                }),
                translate: `0 ${interpolate(frame, [32 + index * 14, 58 + index * 14], [42, 0], {
                  easing: easeOut,
                  extrapolateLeft: "clamp",
                  extrapolateRight: "clamp",
                })}px`,
              }}
            >
              <small>0{index + 1}</small>
              <strong>{step}</strong>
            </div>
          ))}
        </div>
      </div>
    </EditorialFrame>
  );
};

const Equation: React.FC<{vertical?: boolean}> = ({vertical = false}) => {
  const frame = useCurrentFrame();
  return (
    <EditorialFrame index="04" label="THE PRINCIPLE" tone="signal" vertical={vertical}>
      <div className="cc11-equation">
        <p>THE RULE AT THE CENTRE OF THE SYSTEM</p>
        <div>
          <span
            style={{
              opacity: interpolate(frame, [12, 46], [0, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
              translate: `${interpolate(frame, [12, 46], [-90, 0], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              })}px 0`,
            }}
          >
            MEMORY
          </span>
          <em
            style={{
              rotate: `${interpolate(frame, [42, 90], [-12, 0], {
                easing: easeInOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              })}deg`,
              scale: interpolate(frame, [42, 90], [0.4, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
            }}
          >
            ≠
          </em>
          <span
            style={{
              opacity: interpolate(frame, [68, 104], [0, 1], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              }),
              translate: `${interpolate(frame, [68, 104], [90, 0], {
                easing: easeOut,
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
              })}px 0`,
            }}
          >
            AUTHORITY
          </span>
        </div>
        <small
          style={{
            opacity: interpolate(frame, [105, 138], [0, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
          }}
        >
          Human-gated. Receipt-bound. Reversible.
        </small>
      </div>
    </EditorialFrame>
  );
};

const Closing: React.FC<{vertical?: boolean}> = ({vertical = false}) => {
  const frame = useCurrentFrame();
  return (
    <EditorialFrame index="05" label="PRIVATE SHADOW ALPHA" vertical={vertical}>
      <div className="cc11-closing">
        <div
          className="cc11-mark-motion"
          style={{
            opacity: interpolate(frame, [6, 36], [0, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
            scale: interpolate(frame, [6, 36], [0.8, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
          }}
        >
          <span>C</span><span>C</span><i />
        </div>
        <h2
          style={{
            opacity: interpolate(frame, [34, 72], [0, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
            translate: `0 ${interpolate(frame, [34, 72], [60, 0], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            })}px`,
          }}
        >
          A system that can<br/><em>show its work.</em>
        </h2>
        <div
          className="cc11-url"
          style={{
            opacity: interpolate(frame, [82, 116], [0, 1], {
              easing: easeOut,
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
          }}
        >
          <span>CC11 · PRIVATE SHADOW ALPHA</span>
          <strong>cong42.de/cc11/</strong>
        </div>
      </div>
    </EditorialFrame>
  );
};

const Campaign: React.FC<{vertical?: boolean}> = ({vertical = false}) => (
  <AbsoluteFill>
    <Sequence name="01 · Question" durationInFrames={180} premountFor={30}>
      <Opening vertical={vertical}/>
    </Sequence>
    <Sequence name="02 · Boundary" from={180} durationInFrames={180} premountFor={30}>
      <Boundary vertical={vertical}/>
    </Sequence>
    <Sequence name="03 · Evidence path" from={360} durationInFrames={180} premountFor={30}>
      <Flow vertical={vertical}/>
    </Sequence>
    <Sequence name="04 · Principle" from={540} durationInFrames={180} premountFor={30}>
      <Equation vertical={vertical}/>
    </Sequence>
    <Sequence name="05 · Close" from={720} durationInFrames={180} premountFor={30}>
      <Closing vertical={vertical}/>
    </Sequence>
  </AbsoluteFill>
);

export const Cc11Compositions: React.FC = () => (
  <>
    <Composition
      id="CC11Launch"
      component={Campaign}
      durationInFrames={900}
      fps={30}
      width={1920}
      height={1080}
      defaultProps={{vertical: false}}
    />
    <Composition
      id="CC11LaunchVertical"
      component={Campaign}
      durationInFrames={900}
      fps={30}
      width={1080}
      height={1920}
      defaultProps={{vertical: true}}
    />
  </>
);
