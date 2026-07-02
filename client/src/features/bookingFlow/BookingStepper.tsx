import Box from '@mui/material/Box'
import Stepper from '@mui/material/Stepper'
import Step from '@mui/material/Step'
import StepLabel from '@mui/material/StepLabel'
import { useSelector } from 'react-redux'
import type { RootState } from '../../app/store'
import SearchForm from './SearchForm'
import ResultsList from './ResultsList'

const steps = ['Search', 'Select Flight', 'Passenger Details', 'Confirmation']

function BookingStepper() {
  const activeStep = useSelector((state: RootState) => state.bookingFlow.activeStep)

  return (
    <Box sx={{ p: 3 }}>
      <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
        {steps.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {activeStep === 0 && <SearchForm />}
      {activeStep === 1 && <ResultsList />}
      {activeStep === 2 && <Box>{/* Passenger Details - built in the next batch */}</Box>}
      {activeStep === 3 && <Box>{/* Confirmation - built in the next batch */}</Box>}
    </Box>
  )
}

export default BookingStepper
