const localDateTime = (datetime) => { 
  // Check if datetime is valid before processing
  if (!(datetime instanceof Date) || isNaN(datetime)) {
    return new Date(); // Return current date if invalid
  }
  datetime.setMinutes(datetime.getMinutes() - datetime.getTimezoneOffset()); 
  return datetime; 
};

const printDate = (datetime) => { 
  // Handle invalid date values
  if (!(datetime instanceof Date) || isNaN(datetime)) {
    return 'N/A';
  }
  try {
    return datetime.toISOString().slice(0, 10).replace(/-/g, '/'); 
  } catch (error) {
    console.error('Error formatting date:', error);
    return 'Invalid Date';
  }
};

const printDateTime = (datetime) => {
  // Handle invalid date values
  if (!(datetime instanceof Date) || isNaN(datetime)) {
    return 'N/A';
  }
  try {
    const pad = (n) => n.toString().padStart(2, '0');
    return `${datetime.getFullYear()}/${pad(datetime.getMonth() + 1)}/${pad(datetime.getDate())} ${pad(datetime.getHours())}:${pad(datetime.getMinutes())}`;
  } catch (error) {
    console.error('Error formatting date/time:', error);
    return 'Invalid Date';
  }
};

export { localDateTime, printDate, printDateTime }