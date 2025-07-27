

const hexToRgb = (hex) => (value =>
  value.length === 3
    ? value.split('').map(c => parseInt(c.repeat(2), 16))
    : value.match(/.{1,2}/g).map(v => parseInt(v, 16))
  )(hex.replace('#', ''));

const isHexTooDark = (hexColor, maxLightness) => (([r, g, b]) => {
  return (0.2126 * r + 0.7152 * g + 0.0722 * b) < maxLightness
})(hexToRgb(hexColor));

const rgbaToHex = (colorStr, forceRemoveAlpha = false) => {
  // Check if the input string contains '/'
  if (colorStr.indexOf('rgb') >= 0) {
    const hasSlash = colorStr.includes('/')
    if (hasSlash) {
      // Extract the RGBA values from the input string
      const rgbaValues = colorStr.match(/(\d+)\s+(\d+)\s+(\d+)\s+\/\s+([\d.]+)/)

      if (!rgbaValues) {
        return colorStr // Return the original string if it doesn't match the expected format
      }

      const [red, green, blue, alpha] = rgbaValues.slice(1, 5).map(parseFloat)

      // Convert the RGB values to hexadecimal format
      const redHex = red.toString(16).padStart(2, '0')
      const greenHex = green.toString(16).padStart(2, '0')
      const blueHex = blue.toString(16).padStart(2, '0')

      // Convert alpha to a hexadecimal format (assuming it's already a decimal value in the range [0, 1])
      const alphaHex = forceRemoveAlpha
        ? ''
        : Math.round(alpha * 255)
          .toString(16)
          .padStart(2, '0')

      // Combine the hexadecimal values to form the final hex color string
      const hexColor = `#${redHex}${greenHex}${blueHex}${alphaHex}`

      return hexColor
    } else {
      // Use the second code block for the case when '/' is not present
      return (
        colorStr
          .replace(/^rgba?\(|\s+|\)$/g, '') // Get's rgba / rgb string values
          .split(',') // splits them at ","
          .filter((string, index) => !forceRemoveAlpha || index !== 3)
          .map(string => parseFloat(string)) // Converts them to numbers
          .map((number, index) => (index === 3 ? Math.round(number * 255) : number)) // Converts alpha to 255 number
          .map(number => number.toString(16)) // Converts numbers to hex
          .map(string => (string.length === 1 ? '0' + string : string)) // Adds 0 when length of one number is 1
          .join('')
      )
    }
  } else {
    //must be a hex, remove the hash character
    return colorStr.replace('#', '');
  }
}

const hexToHsl = (hex) => {
  // Remove the '#' if it exists
  hex = hex.replace("#", "");

  // Parse r, g, b values, accounting for shorthand hex
  let r = parseInt(hex.length === 3 ? hex[0] + hex[0] : hex.substring(0, 2), 16);
  let g = parseInt(hex.length === 3 ? hex[1] + hex[1] : hex.substring(2, 4), 16);
  let b = parseInt(hex.length === 3 ? hex[2] + hex[2] : hex.substring(4, 6), 16);

  // Normalize to 0-1 range
  r /= 255;
  g /= 255;
  b /= 255;

  // Find min and max values
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  let h, s, l = (max + min) / 2;

  if (max === min) {
    h = s = 0; // achromatic
  } else {
    const d = max - min;
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
    switch (max) {
      case r: h = (g - b) / d + (g < b ? 6 : 0); break;
      case g: h = (b - r) / d + 2; break;
      case b: h = (r - g) / d + 4; break;
    }
    h /= 6;
  }

  h = Math.round(h * 360);
  s = Math.round(s * 100);
  l = Math.round(l * 100);

  return { h, s, l };
}

const hslToHex = ({ h, s, l }) => {
  l /= 100;
  const a = s * Math.min(l, 1 - l) / 100;
  const f = n => {
    const k = (n + h / 30) % 12;
    const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
    return Math.round(255 * color).toString(16).padStart(2, '0');
  };
  return `${f(0)}${f(8)}${f(4)}`;
}

const hexToCmyk = (hex) => {
  // Expand shorthand form (e.g. "03F") to full form (e.g. "0033FF")
  const expandedHex = hex.replace(/^#?([a-f\d])([a-f\d])([a-f\d])$/i,
    function (m, r, g, b) {
      return r + r + g + g + b + b;
    });

  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(expandedHex);
  if (!result) {
    return null;
  }

  const r = parseInt(result[1], 16) / 255;
  const g = parseInt(result[2], 16) / 255;
  const b = parseInt(result[3], 16) / 255;

  const k = 1 - Math.max(r, g, b);
  const c = (1 - r - k) / (1 - k);
  const m = (1 - g - k) / (1 - k);
  const y = (1 - b - k) / (1 - k);

  return {
    c: Math.round(c * 100),
    m: Math.round(m * 100),
    y: Math.round(y * 100),
    k: Math.round(k * 100)
  };
}

const hexToHsv = (hex) => {
  // Remove the '#' if it exists
  hex = hex.replace("#", "");

  // Parse hex values to RGB
  const r = parseInt(hex.substring(0, 2), 16) / 255;
  const g = parseInt(hex.substring(2, 4), 16) / 255;
  const b = parseInt(hex.substring(4, 6), 16) / 255;

  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  let h, s, v = max;

  const d = max - min;
  s = max === 0 ? 0 : d / max;

  if (max === min) {
    h = 0; // achromatic
  } else {
    switch (max) {
      case r: h = (g - b) / d + (g < b ? 6 : 0); break;
      case g: h = (b - r) / d + 2; break;
      case b: h = (r - g) / d + 4; break;
    }
    h /= 6;
  }

  return {
    h: Math.round(h * 360),
    s: Math.round(s * 100),
    v: Math.round(v * 100)
  };
}

const hsvToHex = ({ h, s, v }) => {
  h /= 60;
  s /= 100;
  v /= 100;

  const i = Math.floor(h);
  const f = h - i;
  const p = v * (1 - s);
  const q = v * (1 - f * s);
  const t = v * (1 - (1 - f) * s);

  let r, g, b;
  switch (i % 6) {
    case 0: r = v; g = t; b = p; break;
    case 1: r = q; g = v; b = p; break;
    case 2: r = p; g = v; b = t; break;
    case 3: r = p; g = q; b = v; break;
    case 4: r = t; g = p; b = v; break;
    case 5: r = v; g = p; b = q; break;
  }

  const toHex = (c) => {
    const hex = Math.round(c * 255).toString(16);
    return hex.length === 1 ? "0" + hex : hex;
  };

  const hex = toHex(r) + toHex(g) + toHex(b);

  return hex;
}

export {
  hexToRgb,
  isHexTooDark,
  rgbaToHex,
  hexToHsl,
  hslToHex,
  hexToCmyk,
  hexToHsv,
  hsvToHex
}