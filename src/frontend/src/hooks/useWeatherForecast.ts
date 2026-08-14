import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../lib/apiClient';

export interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

const fetchWeatherForecast = async (): Promise<WeatherForecast[]> => {
  const { data } = await apiClient.get<WeatherForecast[]>('/weatherforecast');
  return data;
};

export function useWeatherForecast() {
  return useQuery({
    queryKey: ['weatherForecast'],
    queryFn: fetchWeatherForecast,
  });
}
