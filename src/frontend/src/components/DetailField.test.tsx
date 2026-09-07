import { render, screen } from '@testing-library/react';
import { DetailField } from './DetailField';

describe('DetailField', () => {
  it('renders label and value', () => {
    render(<DetailField label="Status" value="Active" />);

    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Active')).toBeInTheDocument();
  });
});
