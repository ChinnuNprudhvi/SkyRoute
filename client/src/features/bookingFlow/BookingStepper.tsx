import Container from '@mui/material/Container'
import Stepper from '@mui/material/Stepper'
import Step from '@mui/material/Step'
import StepLabel from '@mui/material/StepLabel'
import SearchIcon from '@mui/icons-material/Search'
import FlightTakeoffIcon from '@mui/icons-material/FlightTakeoff'
import PersonIcon from '@mui/icons-material/Person'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { useSelector } from 'react-redux'
import type { RootState } from '../../app/store'
import SearchForm from './SearchForm'
import ResultsList from './ResultsList'
import PassengerDetailsForm from './PassengerDetailsForm'
import ConfirmationDialog from './ConfirmationDialog'

const steps = [
  { label: 'Search', icon: SearchIcon },
  { label: 'Select Flight', icon: FlightTakeoffIcon },
  { label: 'Passenger Details', icon: PersonIcon },
  { label: 'Confirmation', icon: CheckCircleIcon },
]

function BookingStepper() {
  const activeStep = useSelector((state: RootState) => state.bookingFlow.activeStep)
  const selectedFlightId = useSelector(
    (state: RootState) => state.bookingFlow.selectedFlightId,
  )

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
        {steps.map(({ label, icon: Icon }) => (
          <Step key={label}>
            <StepLabel icon={<Icon color="inherit" />}>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {activeStep === 0 && <SearchForm />}
      {activeStep === 1 && <ResultsList />}
      {activeStep === 2 && <PassengerDetailsForm key={selectedFlightId} />}

      <ConfirmationDialog />
    </Container>
  )
}

export default BookingStepper
