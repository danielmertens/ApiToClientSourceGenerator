export const fetchWeatherForecastGet: () => Promise<WeatherForecast> = async () => {
  const response = await fetch("WeatherForecast/GetWeatherForecast");
  const body = await response.json();
  return body;
}

export const fetchWeatherForecastTest: () => Promise<> = async () => {
  const response = await fetch("WeatherForecast/Test");
  const body = await response.json();
  return body;
}

export type WeatherForecast = {
  date: Date
  temperatureC: number
  temperatureF: number
  summary: any
}

