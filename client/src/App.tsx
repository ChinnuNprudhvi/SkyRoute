import Box from '@mui/material/Box'
import ApiHealthGate from './shared/ApiHealthGate'
import GlobalSnackbar from './shared/GlobalSnackbar'

function App() {
  return (
    <>
      <ApiHealthGate>
        <Box sx={{ p: 3 }}>{/* Stepper goes here in the next batch */}</Box>
      </ApiHealthGate>
      <GlobalSnackbar />
    </>
  )
}

export default App
