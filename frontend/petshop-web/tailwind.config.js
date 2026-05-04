/** @type {import('tailwindcss').Config} */
export default {
  darkMode: "class",
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: "#6366f1",
          hover: "#4f46e5",
          light: "#a5b4fc",
          muted: "rgba(99,102,241,0.12)",
        },
        surface: {
          DEFAULT: "#ffffff",
          2: "#f3f4f6",
          3: "#e5e7eb",
        },
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
      },
    },
  },
  plugins: [],
};
