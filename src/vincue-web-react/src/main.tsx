import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
/* 2. src/main.tsx — the actual bootstrap. This is the direct equivalent of Angular's main.ts (bootstrapApplication(App, appConfig)), 
but notice the shape: createRoot(document.getElementById('root')!).render(<App />). 
React grabs a specific DOM node and renders into it; 
there's no separate AppConfig/providers file to point at, because this app has nothing to configure (no router, no DI providers) 
— if it needed global providers (a router, a context provider), they'd wrap <App /> right here, in this same call, rather than in a separate config file. */
