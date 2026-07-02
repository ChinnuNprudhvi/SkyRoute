import Snackbar from '@mui/material/Snackbar'
import Alert from '@mui/material/Alert'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState } from '../app/store'
import { hideSnackbar } from '../features/ui/uiSlice'

function GlobalSnackbar() {
  const dispatch = useDispatch()
  const snackbar = useSelector((state: RootState) => state.ui.snackbar)

  return (
    <Snackbar
      open={snackbar.open}
      autoHideDuration={5000}
      onClose={() => dispatch(hideSnackbar())}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
    >
      <Alert
        onClose={() => dispatch(hideSnackbar())}
        severity={snackbar.severity}
        variant="filled"
        sx={{ width: '100%' }}
      >
        {snackbar.message}
      </Alert>
    </Snackbar>
  )
}

export default GlobalSnackbar
