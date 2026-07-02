import { Component, type ErrorInfo, type ReactNode } from 'react'
import ServiceUnavailable from './ServiceUnavailable'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Unhandled error caught by ErrorBoundary:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      return (
        <ServiceUnavailable onRetry={() => window.location.reload()} />
      )
    }

    return this.props.children
  }
}

export default ErrorBoundary
