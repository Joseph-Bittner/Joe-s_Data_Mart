/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./Pages/**/*.cshtml",
    "./**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        "datamart-primary": "#2b3152",
        "datamart-secondary": "#4d5581"
      }
    }
  },
  plugins: []
};
