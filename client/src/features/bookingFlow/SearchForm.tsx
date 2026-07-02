import { useRef, useState } from 'react'
import Paper from '@mui/material/Paper'
import Grid from '@mui/material/Grid'
import FormControl from '@mui/material/FormControl'
import InputLabel from '@mui/material/InputLabel'
import Select from '@mui/material/Select'
import MenuItem from '@mui/material/MenuItem'
import TextField from '@mui/material/TextField'
import FormHelperText from '@mui/material/FormHelperText'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import InputAdornment from '@mui/material/InputAdornment'
import IconButton from '@mui/material/IconButton'
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import { useDispatch } from 'react-redux'
import { useGetAirportsQuery, useSearchFlightsMutation } from '../api/skyRouteApi'
import { showSnackbar } from '../ui/uiSlice'

const CABIN_CLASSES = ['Economy', 'Business', 'First']

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

function SearchForm() {
  const dispatch = useDispatch()
  const { data: airports = [] } = useGetAirportsQuery()
  const [searchFlights, { isLoading }] = useSearchFlightsMutation()

  const [originCode, setOriginCode] = useState('')
  const [destinationCode, setDestinationCode] = useState('')
  const [departureDate, setDepartureDate] = useState('')
  const [passengers, setPassengers] = useState(1)
  const [cabinClass, setCabinClass] = useState(CABIN_CLASSES[0])
  const departureDateInputRef = useRef<HTMLInputElement>(null)

  const sameAirportError =
    originCode !== '' && destinationCode !== '' && originCode === destinationCode

  const canSearch =
    originCode !== '' && destinationCode !== '' && !sameAirportError && departureDate !== ''

  const handleSearch = async () => {
    if (!canSearch) {
      return
    }

    try {
      await searchFlights({
        originCode,
        destinationCode,
        departureDate,
        passengers,
        cabinClass,
      }).unwrap()
    } catch {
      dispatch(
        showSnackbar({
          message: 'Something went wrong while searching for flights. Please try again.',
          severity: 'error',
        }),
      )
    }
  }

  return (
    <Paper elevation={3} sx={{ maxWidth: 800, p: 3 }}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <FormControl fullWidth>
            <InputLabel id="origin-label">Origin</InputLabel>
            <Select
              labelId="origin-label"
              label="Origin"
              value={originCode}
              onChange={(e) => setOriginCode(e.target.value)}
            >
              {airports.map((airport) => (
                <MenuItem key={airport.code} value={airport.code}>
                  {airport.city} ({airport.code})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <FormControl fullWidth error={sameAirportError}>
            <InputLabel id="destination-label">Destination</InputLabel>
            <Select
              labelId="destination-label"
              label="Destination"
              value={destinationCode}
              onChange={(e) => setDestinationCode(e.target.value)}
            >
              {airports.map((airport) => (
                <MenuItem key={airport.code} value={airport.code}>
                  {airport.city} ({airport.code})
                </MenuItem>
              ))}
            </Select>
            {sameAirportError && (
              <FormHelperText>Origin and destination must be different.</FormHelperText>
            )}
          </FormControl>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            label="Departure date"
            type="date"
            fullWidth
            required
            inputRef={departureDateInputRef}
            slotProps={{
              inputLabel: { shrink: true },
              htmlInput: { min: todayIsoDate() },
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      aria-label="Open date picker"
                      edge="end"
                      onClick={() => departureDateInputRef.current?.showPicker?.()}
                    >
                      <CalendarMonthIcon />
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }}
            value={departureDate}
            onChange={(e) => setDepartureDate(e.target.value)}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <TextField
            label="Passengers"
            type="number"
            fullWidth
            slotProps={{ htmlInput: { min: 1, max: 9 } }}
            value={passengers}
            onChange={(e) => setPassengers(Number(e.target.value))}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <FormControl fullWidth>
            <InputLabel id="cabin-class-label">Cabin class</InputLabel>
            <Select
              labelId="cabin-class-label"
              label="Cabin class"
              value={cabinClass}
              onChange={(e) => setCabinClass(e.target.value)}
            >
              {CABIN_CLASSES.map((option) => (
                <MenuItem key={option} value={option}>
                  {option}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Button
            variant="contained"
            disabled={!canSearch || isLoading}
            onClick={handleSearch}
            startIcon={isLoading ? <CircularProgress size={16} color="inherit" /> : undefined}
          >
            Search
          </Button>
        </Grid>
      </Grid>
    </Paper>
  )
}

export default SearchForm
