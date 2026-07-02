import Dialog from '@mui/material/Dialog'
import DialogContent from '@mui/material/DialogContent'
import DialogActions from '@mui/material/DialogActions'
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import Button from '@mui/material/Button'
import Divider from '@mui/material/Divider'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState } from '../../app/store'
import { resetFlow } from './bookingFlowSlice'

function ConfirmationDialog() {
  const dispatch = useDispatch()
  const activeStep = useSelector((state: RootState) => state.bookingFlow.activeStep)
  const bookingResult = useSelector((state: RootState) => state.bookingFlow.bookingResult)

  const open = activeStep === 3 && bookingResult !== null

  return (
    <Dialog open={open} maxWidth="sm" fullWidth>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1, py: 2 }}>
          <CheckCircleIcon color="success" sx={{ fontSize: 64 }} />
          <Typography variant="h5" component="h2">
            Booking Confirmed!
          </Typography>
          <Typography
            variant="h5"
            sx={{ fontFamily: 'monospace', fontWeight: 'bold', mt: 1 }}
          >
            {bookingResult?.bookingReference}
          </Typography>
        </Box>

        {bookingResult && (
          <>
            <Divider sx={{ my: 2 }} />
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
              <Typography variant="body2">
                {bookingResult.flightSummary.provider} · {bookingResult.flightSummary.flightNumber}
              </Typography>
              <Typography variant="body2">
                {bookingResult.flightSummary.origin} → {bookingResult.flightSummary.destination}
              </Typography>
              <Typography variant="body2">
                Departs: {bookingResult.flightSummary.departureTime}
              </Typography>
              <Typography variant="body2">
                Arrives: {bookingResult.flightSummary.arrivalTime}
              </Typography>
              <Typography variant="body2">
                Cabin: {bookingResult.flightSummary.cabinClass}
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 'bold', mt: 1 }}>
                Total Price: {bookingResult.totalPrice.toFixed(2)}
              </Typography>
            </Box>
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={() => dispatch(resetFlow())}>
          Book Another
        </Button>
      </DialogActions>
    </Dialog>
  )
}

export default ConfirmationDialog
