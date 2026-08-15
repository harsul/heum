import { useQuery } from '@tanstack/react-query';
import { fetchWeatherForecast } from '../api/weatherApi';

export function useWeatherForecast() {
  return useQuery({
    queryKey: ['weatherForecast'],
    queryFn: fetchWeatherForecast,
  });
}
