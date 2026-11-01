// Spelled exactly like CRM.Domain/Constants/VettingOptions.cs — the API rejects anything else.
// ponytail: duplicates the C# lists. Fine while they are fixed; if they start changing, serve them
// from a GET /vetting/options endpoint instead.

export const PROVINCES = [
  'Eastern Cape', 'Free State', 'Gauteng', 'KwaZulu-Natal', 'Limpopo',
  'Mpumalanga', 'North West', 'Northern Cape', 'Western Cape',
] as const;

export const CORE_FOCUS_AREAS = ['Feeding', 'ECD', 'Education', 'Health', 'Shelter', 'Skills training', 'Care & support'] as const;
export const TARGET_POPULATIONS = ['Children', 'Youth', 'Women', 'Elderly', 'Disabled', 'Homeless', 'Families'] as const;
export const DISTRIBUTION_CHANNELS = ['Direct to beneficiaries', 'Through other organisations', 'Both'] as const;
export const RACES = ['African', 'Coloured', 'Indian/Asian', 'White'] as const;
export const GENDERS = ['Female', 'Male', 'Other'] as const;
export const AGE_GROUPS = ['0–5', '6–12', '13–18', '19–35', '36–59', '60+'] as const;
export const FEEDING_FREQUENCIES = ['Daily', 'Weekdays only', 'Weekly', 'Twice a week', 'Monthly'] as const;
export const RELIANCE_LEVELS = ['Low', 'Moderate', 'High', 'Total'] as const;
export const TRANSPORT_CAPACITIES = ['Own vehicle', 'Hired transport', 'None — delivery required'] as const;
export const MEALS = ['Breakfast', 'Lunch', 'Dinner'] as const;
export const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'] as const;
export const STORAGE_TYPES = ['Dry', 'Chilled', 'Frozen'] as const;
export const INFRASTRUCTURE_CHECKS = ['Roofing', 'Fencing', 'Lighting', 'Gas safety', 'Electrics'] as const;
