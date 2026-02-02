export interface Position {
  x: number;
  y: number;
}
export interface Stroke {
  id: string;
  points: Position[];
  color: string;
  size: number;
  strokeDate: string;
  visible: boolean;
}
