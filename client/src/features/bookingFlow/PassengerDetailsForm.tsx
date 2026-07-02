import { useState } from 'react'
import Box from '@mui/material/Box'
import Paper from '@mui/material/Paper'
import Grid from '@mui/material/Grid'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState } from '../../app/store'
import type { PassengerInput } from '../../shared/types'
import { useCreateBookingMutation } from '../api/skyRouteApi'
import { showSnackbar } from '../ui/uiSlice'
import { selectSelectedFlightOffer, setActiveStep, setBookingResult } from './bookingFlowSlice'

const INTERNATIONAL_DOCUMENT_PATTERN = /^[A-Za-z0-9]{6,9}$/
const DOMESTIC_DOCUMENT_PATTERN = /^\d{6,12}$/

function isFetchBaseQueryErrorWithStatus(
  error: unknown,
): error is { status: number } {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    typeof (error as { status: unknown }).status === 'number'
  )
}

function PassengerDetailsForm() {
  const dispatch = useDispatch()
  const selectedOffer = useSelector(selectSelectedFlightOffer)
  const searchId = useSelector((state: RootState) => state.bookingFlow.searchId)
  const selectedFlightId = useSelector(
    (state: RootState) => state.bookingFlow.selectedFlightId,
  )
  const isInternational = useSelector(
    (state: RootState) => state.bookingFlow.isInternational,
  )
  const passengerCount = useSelector(
    (state: RootState) => state.bookingFlow.lastSearchCriteria?.passengers ?? 1,
  )

  const [createBooking, { isLoading }] = useCreateBookingMutation()

  const [passengers, setPassengers] = useState<PassengerInput[]>(() =>
    Array.from({ length: passengerCount }, () => ({
      fullName: '',
      email: '',
      documentNumber: '',
    })),
  )
  const [documentErrors, setDocumentErrors] = useState<boolean[]>(() =>
    Array.from({ length: passengerCount }, () => false),
  )

  const documentLabel = isInternational ? 'Passport Number' : 'National ID'
  const documentPattern = isInternational
    ? INTERNATIONAL_DOCUMENT_PATTERN
    : DOMESTIC_DOCUMENT_PATTERN

  const updatePassenger = (index: number, field: keyof PassengerInput, value: string) => {
    setPassengers((prev) =>
      prev.map((passenger, i) => (i === index ? { ...passenger, [field]: value } : passenger)),
    )
  }

  const handleDocumentBlur = (index: number) => {
    const value = passengers[index].documentNumber
    setDocumentErrors((prev) =>
      prev.map((hasError, i) => (i === index ? !documentPattern.test(value) : hasError)),
    )
  }

  const allFieldsFilled = passengers.every(
    (passenger) =>
      passenger.fullName.trim() !== '' &&
      passenger.email.trim() !== '' &&
      passenger.documentNumber.trim() !== '',
  )
  const hasDocumentError = documentErrors.some(Boolean)
  const canConfirm = allFieldsFilled && !hasDocumentError

  const handleConfirm = async () => {
    if (!canConfirm || searchId === null || selectedFlightId === null) {
      return
    }

    try {
      const response = await createBooking({
        searchId,
        flightId: selectedFlightId,
        passengers,
      }).unwrap()

      dispatch(setBookingResult(response))
      dispatch(setActiveStep(3))
    } catch (error) {
      if (isFetchBaseQueryErrorWithStatus(error) && error.status === 410) {
        dispatch(
          showSnackbar({
            message: 'Your search has expired — please search again.',
            severity: 'warning',
          }),
        )
        dispatch(setActiveStep(0))
      } else {
        dispatch(
          showSnackbar({
            message: 'Booking failed, please check your details and try again.',
            severity: 'error',
          }),
        )
      }
    }
  }

  return (
    <Box>
      <Box sx={{ mb: 2 }}>
        <Button
          variant="text"
          startIcon={<ArrowBackIcon />}
          onClick={() => dispatch(setActiveStep(1))}
        >
          Change Flight
        </Button>
      </Box>

      {selectedOffer && (
        <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
          <Typography variant="body1">
            {selectedOffer.currency} {selectedOffer.pricePerPerson.toFixed(2)} x{' '}
            {passengerCount} passengers
          </Typography>
          <Typography variant="h6" sx={{ fontWeight: 'bold', mt: 1 }}>
            Total: {selectedOffer.currency} {selectedOffer.totalPrice.toFixed(2)}
          </Typography>
        </Paper>
      )}

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        {passengers.map((passenger, index) => (
          <Paper key={index} elevation={2} sx={{ p: 3 }}>
            <Typography variant="subtitle1" sx={{ mb: 2 }}>
              Passenger {index + 1}
            </Typography>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField
                  label="Full Name"
                  fullWidth
                  required
                  value={passenger.fullName}
                  onChange={(e) => updatePassenger(index, 'fullName', e.target.value)}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField
                  label="Email"
                  type="email"
                  fullWidth
                  required
                  value={passenger.email}
                  onChange={(e) => updatePassenger(index, 'email', e.target.value)}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField
                  label={documentLabel}
                  fullWidth
                  required
                  value={passenger.documentNumber}
                  onChange={(e) => updatePassenger(index, 'documentNumber', e.target.value)}
                  onBlur={() => handleDocumentBlur(index)}
                  error={documentErrors[index]}
                  helperText={
                    documentErrors[index]
                      ? isInternational
                        ? 'Enter 6-9 alphanumeric characters.'
                        : 'Enter 6-12 digits.'
                      : ' '
                  }
                />
              </Grid>
            </Grid>
          </Paper>
        ))}
      </Box>

      <Box sx={{ mt: 3 }}>
        <Button
          variant="contained"
          disabled={!canConfirm || isLoading}
          onClick={handleConfirm}
          startIcon={isLoading ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          Confirm Booking
        </Button>
      </Box>
    </Box>
  )
}

export default PassengerDetailsForm
