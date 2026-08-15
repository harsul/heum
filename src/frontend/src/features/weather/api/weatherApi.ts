import { apiClient } from '../../../lib/apiClient';
import type { WeatherForecast } from '../types/weather';

export async function fetchWeatherForecast(): Promise<WeatherForecast[]> {
  const { data } = await apiClient.get<WeatherForecast[]>('/weatherforecast');
  return data;
}
