import "./index.css";
import "./cc11.css";
import { MyComposition } from "./Composition";
import { Cc11Compositions } from "./Cc11Campaign";

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <MyComposition />
      <Cc11Compositions />
    </>
  );
};
