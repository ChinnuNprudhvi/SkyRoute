import ApiHealthGate from './shared/ApiHealthGate'
import GlobalSnackbar from './shared/GlobalSnackbar'
import BookingStepper from './features/bookingFlow/BookingStepper'

function App() {
  return (
    <>
      <ApiHealthGate>
        <BookingStepper />
      </ApiHealthGate>
      <GlobalSnackbar />
    </>
  )
}

export default App
