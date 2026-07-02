import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import Button from '@mui/material/Button'

interface ServiceUnavailableProps {
  onRetry: () => void
}

function ServiceUnavailable({ onRetry }: ServiceUnavailableProps) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        height: '100vh',
        textAlign: 'center',
        px: 3,
      }}
    >
      <Typography variant="h5" component="h1">
        We're having trouble right now
      </Typography>
      <Typography variant="body1" color="text.secondary">
        Our team is working to sort this out — please try again in a moment.
      </Typography>
      <Button variant="contained" onClick={onRetry}>
        Retry
      </Button>
    </Box>
  )
}

export default ServiceUnavailable
