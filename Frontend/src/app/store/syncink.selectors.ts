import { createFeature, createFeatureSelector, createSelector } from '@ngrx/store';
import { BrushState } from './syncink.reducer';

export const selectBrushState = createFeatureSelector<BrushState>('brush');

export const selectBrushColor = createSelector(selectBrushState, (state) => state.color);

export const selectBrushSize = createSelector(selectBrushState, (state) => state.size);
