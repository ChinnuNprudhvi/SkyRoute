import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type {
  Airport,
  BookingResponse,
  CreateBookingRequest,
  FlightSearchRequest,
  FlightSearchResponse,
} from '../../shared/types'

export const skyRouteApi = createApi({
  reducerPath: 'skyRouteApi',
  baseQuery: fetchBaseQuery({ baseUrl: '/api' }),
  endpoints: (builder) => ({
    getAirports: builder.query<Airport[], void>({
      query: () => '/airports',
    }),
    searchFlights: builder.mutation<FlightSearchResponse, FlightSearchRequest>({
      query: (body) => ({
        url: '/flights/search',
        method: 'POST',
        body,
      }),
    }),
    createBooking: builder.mutation<BookingResponse, CreateBookingRequest>({
      query: (body) => ({
        url: '/bookings',
        method: 'POST',
        body,
      }),
    }),
  }),
})

export const {
  useGetAirportsQuery,
  useSearchFlightsMutation,
  useCreateBookingMutation,
} = skyRouteApi
