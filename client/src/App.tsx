import ApiHealthGate from './shared/ApiHealthGate'
import AppHeader from './shared/AppHeader'
import GlobalSnackbar from './shared/GlobalSnackbar'
import BookingStepper from './features/bookingFlow/BookingStepper'

function App() {
  return (
    <>
      <AppHeader />
      <ApiHealthGate>
        <BookingStepper />
      </ApiHealthGate>
      <GlobalSnackbar />
    </>
  )
}

export default App
