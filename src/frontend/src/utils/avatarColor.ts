const PALETTE = [
  '#1890FF', '#54D62C', '#FFC107', '#FF4842',
  '#04297A', '#7A0C2E', '#006C9C', '#7635DC',
];

export function stringToColor(str: string): string {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  return PALETTE[Math.abs(hash) % PALETTE.length];
}
