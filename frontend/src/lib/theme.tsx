import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

export type ThemeName = 'default' | 'hogwarts'
export type HouseId = 'gryffindor' | 'slytherin' | 'ravenclaw' | 'hufflepuff'

export const HOUSES: { id: HouseId; name: string; color: string }[] = [
  { id: 'gryffindor', name: 'Grifinória', color: '#ae2e24' },
  { id: 'slytherin', name: 'Sonserina', color: '#1f7a52' },
  { id: 'ravenclaw', name: 'Corvinal', color: '#2f5fa8' },
  { id: 'hufflepuff', name: 'Lufa-Lufa', color: '#d4a53f' },
]

const T_KEY = 'wvm.theme'
const H_KEY = 'wvm.house'

type Ctx = {
  theme: ThemeName
  house: HouseId
  houseColor: string
  isHogwarts: boolean
  setTheme: (t: ThemeName) => void
  setHouse: (h: HouseId) => void
}

const ThemeContext = createContext<Ctx | null>(null)

function readTheme(): ThemeName {
  return localStorage.getItem(T_KEY) === 'hogwarts' ? 'hogwarts' : 'default'
}
function readHouse(): HouseId {
  const raw = localStorage.getItem(H_KEY)
  return HOUSES.some((h) => h.id === raw) ? (raw as HouseId) : 'gryffindor'
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<ThemeName>(readTheme)
  const [house, setHouseState] = useState<HouseId>(readHouse)

  // Dirige o CSS: as variáveis de cor do Tailwind são sobrescritas por [data-theme]/[data-house],
  // então trocar esses atributos re-tematiza o app inteiro de uma vez (ver index.css).
  useEffect(() => {
    document.documentElement.dataset.theme = theme
    document.documentElement.dataset.house = house
  }, [theme, house])

  const setTheme = (t: ThemeName) => {
    setThemeState(t)
    localStorage.setItem(T_KEY, t)
  }
  const setHouse = (h: HouseId) => {
    setHouseState(h)
    localStorage.setItem(H_KEY, h)
  }

  const houseColor = HOUSES.find((h) => h.id === house)!.color

  return (
    <ThemeContext.Provider
      value={{ theme, house, houseColor, isHogwarts: theme === 'hogwarts', setTheme, setHouse }}
    >
      {children}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error('useTheme precisa estar dentro do ThemeProvider')
  return ctx
}
