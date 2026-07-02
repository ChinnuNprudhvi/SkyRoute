import { configureStore } from '@reduxjs/toolkit'
import { skyRouteApi } from '../features/api/skyRouteApi'
import bookingFlowReducer from '../features/bookingFlow/bookingFlowSlice'
import uiReducer from '../features/ui/uiSlice'

export const store = configureStore({
  reducer: {
    [skyRouteApi.reducerPath]: skyRouteApi.reducer,
    bookingFlow: bookingFlowReducer,
    ui: uiReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(skyRouteApi.middleware),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
