import { createSlice, type PayloadAction } from '@reduxjs/toolkit'

export type SnackbarSeverity = 'success' | 'error' | 'warning' | 'info'

interface SnackbarState {
  open: boolean
  message: string
  severity: SnackbarSeverity
}

interface UiState {
  snackbar: SnackbarState
}

const initialState: UiState = {
  snackbar: {
    open: false,
    message: '',
    severity: 'info',
  },
}

const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    showSnackbar(
      state,
      action: PayloadAction<{ message: string; severity: SnackbarSeverity }>,
    ) {
      state.snackbar.open = true
      state.snackbar.message = action.payload.message
      state.snackbar.severity = action.payload.severity
    },
    hideSnackbar(state) {
      state.snackbar.open = false
    },
  },
})

export const { showSnackbar, hideSnackbar } = uiSlice.actions

export default uiSlice.reducer
