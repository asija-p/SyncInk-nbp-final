import { createReducer, on } from '@ngrx/store';
import { setColor, setSize } from './syncink.actions';

export interface BrushState {
  color: string;
  size: number;
}

export const initialBrushState: BrushState = {
  color: '#000000',
  size: 20,
};

export const brushColorReducer = createReducer(
  initialBrushState,
  on(setColor, (state, { color }) => ({ ...state, color })),
  on(setSize, (state, { size }) => ({ ...state, size }))
);
