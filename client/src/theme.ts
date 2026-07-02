import { createTheme } from '@mui/material/styles'

const backgroundDefault = '#F4F7FA'

const theme = createTheme({
  palette: {
    primary: {
      main: '#0B5FA5',
    },
    background: {
      default: backgroundDefault,
    },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: `
        body {
          background-color: ${backgroundDefault};
          background-image: radial-gradient(circle, rgba(11, 95, 165, 0.06) 1px, transparent 1px);
          background-size: 20px 20px;
        }
      `,
    },
  },
})

export default theme
