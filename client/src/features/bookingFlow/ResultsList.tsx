import { useEffect, useMemo, useRef, useState } from 'react'
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import Button from '@mui/material/Button'
import Table from '@mui/material/Table'
import TableHead from '@mui/material/TableHead'
import TableBody from '@mui/material/TableBody'
import TableRow from '@mui/material/TableRow'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableSortLabel from '@mui/material/TableSortLabel'
import Paper from '@mui/material/Paper'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState } from '../../app/store'
import type { FlightOffer } from '../../shared/types'
import { resetFlow, selectFlight } from './bookingFlowSlice'
import { showSnackbar } from '../ui/uiSlice'

type SortKey = 'provider' | 'departureTime' | 'durationMinutes' | 'totalPrice'
type SortDirection = 'asc' | 'desc'

function ResultsList() {
  const dispatch = useDispatch()
  const results = useSelector((state: RootState) => state.bookingFlow.results)
  const partialResults = useSelector((state: RootState) => state.bookingFlow.partialResults)
  const notice = useSelector((state: RootState) => state.bookingFlow.notice)

  const [sortKey, setSortKey] = useState<SortKey>('totalPrice')
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')

  const notifiedRef = useRef(false)

  useEffect(() => {
    if (partialResults && !notifiedRef.current) {
      notifiedRef.current = true
      dispatch(showSnackbar({ message: notice ?? '', severity: 'warning' }))
    }
  }, [partialResults, notice, dispatch])

  const sortedResults = useMemo(() => {
    const factor = sortDirection === 'asc' ? 1 : -1
    return [...results].sort((a, b) => {
      const aValue = a[sortKey]
      const bValue = b[sortKey]
      if (typeof aValue === 'string' && typeof bValue === 'string') {
        return aValue.localeCompare(bValue) * factor
      }
      return ((aValue as number) - (bValue as number)) * factor
    })
  }, [results, sortKey, sortDirection])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(key)
      setSortDirection('asc')
    }
  }

  const handleSelect = (offer: FlightOffer) => {
    dispatch(selectFlight(offer.flightId))
  }

  const newSearchButton = (
    <Button variant="outlined" onClick={() => dispatch(resetFlow())}>
      New Search
    </Button>
  )

  if (results.length === 0) {
    return (
      <Paper
        elevation={3}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 2,
          py: 6,
        }}
      >
        <Typography variant="body1">No flights match your search.</Typography>
        {newSearchButton}
      </Paper>
    )
  }

  return (
    <Box>
      <Box sx={{ mb: 2 }}>{newSearchButton}</Box>
      <TableContainer component={Paper} elevation={3}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>
                <TableSortLabel
                  active={sortKey === 'provider'}
                  direction={sortKey === 'provider' ? sortDirection : 'asc'}
                  onClick={() => handleSort('provider')}
                >
                  Provider
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortKey === 'departureTime'}
                  direction={sortKey === 'departureTime' ? sortDirection : 'asc'}
                  onClick={() => handleSort('departureTime')}
                >
                  Flight
                </TableSortLabel>
              </TableCell>
              <TableCell>Departure</TableCell>
              <TableCell>Arrival</TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortKey === 'durationMinutes'}
                  direction={sortKey === 'durationMinutes' ? sortDirection : 'asc'}
                  onClick={() => handleSort('durationMinutes')}
                >
                  Duration
                </TableSortLabel>
              </TableCell>
              <TableCell>Cabin</TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortKey === 'totalPrice'}
                  direction={sortKey === 'totalPrice' ? sortDirection : 'asc'}
                  onClick={() => handleSort('totalPrice')}
                >
                  Price
                </TableSortLabel>
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sortedResults.map((offer) => (
              <TableRow
                key={offer.flightId}
                hover
                sx={{ cursor: 'pointer' }}
                onClick={() => handleSelect(offer)}
              >
                <TableCell>{offer.provider}</TableCell>
                <TableCell>{offer.flightNumber}</TableCell>
                <TableCell>{offer.departureTime}</TableCell>
                <TableCell>{offer.arrivalTime}</TableCell>
                <TableCell>{offer.durationMinutes} min</TableCell>
                <TableCell>{offer.cabinClass}</TableCell>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                    {offer.currency} {offer.totalPrice.toFixed(2)}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {offer.currency} {offer.pricePerPerson.toFixed(2)} per person
                  </Typography>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  )
}

export default ResultsList
