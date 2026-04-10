/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.{razor,html,cshtml}"
  ],
  theme: {
    extend: {
      colors: {
        mattel: {
          red: '#EE0024',
          hover: '#D10020',
          steel: '#62656A',
          light: '#ADAEB2'
        }
      }
    },
  },
  plugins: [],
}

