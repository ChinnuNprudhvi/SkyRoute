import type { ReactNode } from 'react'
import Box from '@mui/material/Box'
import CircularProgress from '@mui/material/CircularProgress'
import { useGetAirportsQuery } from '../features/api/skyRouteApi'
import ServiceUnavailable from './ServiceUnavailable'

interface ApiHealthGateProps {
  children: ReactNode
}

function ApiHealthGate({ children }: ApiHealthGateProps) {
  const { isLoading, isError, refetch } = useGetAirportsQuery()

  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          height: '100vh',
        }}
      >
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return <ServiceUnavailable onRetry={() => refetch()} />
  }

  return children
}

export default ApiHealthGate
