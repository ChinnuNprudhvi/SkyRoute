import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { skyRouteApi } from '../api/skyRouteApi'
import type { FlightOffer, PassengerInput } from '../../shared/types'

interface BookingFlowState {
  activeStep: number
  searchId: string | null
  isInternational: boolean
  results: FlightOffer[]
  partialResults: boolean
  notice: string | null
  selectedFlightId: string | null
  passengers: PassengerInput[]
}

const initialState: BookingFlowState = {
  activeStep: 0,
  searchId: null,
  isInternational: false,
  results: [],
  partialResults: false,
  notice: null,
  selectedFlightId: null,
  passengers: [],
}

const bookingFlowSlice = createSlice({
  name: 'bookingFlow',
  initialState,
  reducers: {
    setActiveStep(state, action: PayloadAction<number>) {
      state.activeStep = action.payload
    },
    selectFlight(state, action: PayloadAction<string>) {
      state.selectedFlightId = action.payload
      state.activeStep = 2
    },
    setPassengers(state, action: PayloadAction<PassengerInput[]>) {
      state.passengers = action.payload
    },
    resetFlow() {
      return initialState
    },
  },
  extraReducers: (builder) => {
    builder.addMatcher(
      skyRouteApi.endpoints.searchFlights.matchFulfilled,
      (state, action) => {
        state.searchId = action.payload.searchId
        state.isInternational = action.payload.isInternational
        state.results = action.payload.results
        state.partialResults = action.payload.partialResults
        state.notice = action.payload.notice
        state.activeStep = 1
      },
    )
  },
})

export const { setActiveStep, selectFlight, setPassengers, resetFlow } =
  bookingFlowSlice.actions

export default bookingFlowSlice.reducer
