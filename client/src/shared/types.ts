export interface Airport {
  code: string
  name: string
  city: string
  country: string
}

export interface FlightSearchRequest {
  originCode: string
  destinationCode: string
  departureDate: string
  passengers: number
  cabinClass: string
}

export interface FlightOffer {
  flightId: string
  provider: string
  flightNumber: string
  departureTime: string
  arrivalTime: string
  durationMinutes: number
  cabinClass: string
  totalPrice: number
  pricePerPerson: number
  currency: string
}

export interface FlightSearchResponse {
  searchId: string
  isInternational: boolean
  results: FlightOffer[]
  partialResults: boolean
  notice: string | null
}

export interface PassengerInput {
  fullName: string
  email: string
  documentNumber: string
}

export interface CreateBookingRequest {
  searchId: string
  flightId: string
  passengers: PassengerInput[]
}

export interface BookingResponse {
  bookingReference: string
  status: string
  totalPrice: number
  flightSummary: {
    provider: string
    flightNumber: string
    origin: string
    destination: string
    departureTime: string
    arrivalTime: string
    cabinClass: string
  }
}
