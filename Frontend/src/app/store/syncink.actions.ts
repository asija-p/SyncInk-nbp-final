import { createAction, props } from '@ngrx/store';

export const setColor = createAction('[Brush] Set Color', props<{ color: string }>());

export const setSize = createAction('[Brush] Set Size', props<{ size: number }>());
