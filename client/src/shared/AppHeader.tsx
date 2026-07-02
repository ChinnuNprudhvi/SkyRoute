import AppBar from '@mui/material/AppBar'
import Toolbar from '@mui/material/Toolbar'
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import FlightTakeoffIcon from '@mui/icons-material/FlightTakeoff'

function AppHeader() {
  return (
    <AppBar position="static" color="primary">
      <Toolbar>
        <FlightTakeoffIcon sx={{ mr: 2 }} />
        <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1 }}>
          <Typography variant="h6" component="span" sx={{ fontWeight: 'bold' }}>
            SkyRoute
          </Typography>
          <Typography variant="caption" sx={{ opacity: 0.8 }}>
            Travel Platform
          </Typography>
        </Box>
      </Toolbar>
    </AppBar>
  )
}

export default AppHeader
