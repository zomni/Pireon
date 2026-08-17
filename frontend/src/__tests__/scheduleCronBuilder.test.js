import {
  buildCron,
  isValidCron,
  describeCron,
  DAYS_OF_WEEK,
  WEEKDAY_LABELS,
} from "../utils/scheduleCronBuilder.js";

describe("scheduleCronBuilder", () => {
  describe("DAYS_OF_WEEK and labels", () => {
    test("exposes all seven weekday keys", () => {
      expect(DAYS_OF_WEEK).toHaveLength(7);
      expect(DAYS_OF_WEEK).toEqual(["lu", "ma", "mi", "ju", "vi", "sa", "do"]);
    });

    test("maps every day key to a Spanish label", () => {
      DAYS_OF_WEEK.forEach((day) => {
        expect(WEEKDAY_LABELS[day]).toBeTruthy();
      });
    });
  });

  describe("buildCron", () => {
    test("builds a daily cron by default", () => {
      expect(buildCron({ time: "08:30" })).toBe("30 08 * * *");
    });

    test("builds a daily cron at midnight", () => {
      expect(buildCron({ frequency: "daily", time: "00:00" })).toBe("00 00 * * *");
    });

    test("builds a weekly cron from selected days", () => {
      const cron = buildCron({ frequency: "weekly", daysOfWeek: ["lu", "ju", "vi"], time: "13:30" });
      expect(cron).toBe("30 13 * * 1,4,5");
    });

    test("sorts weekly days regardless of input order", () => {
      const cron = buildCron({ frequency: "weekly", daysOfWeek: ["vi", "lu", "ma"], time: "08:30" });
      expect(cron).toBe("30 08 * * 1,2,5");
    });

    test("maps sunday to cron day 0", () => {
      const cron = buildCron({ frequency: "weekly", daysOfWeek: ["do"], time: "09:15" });
      expect(cron).toBe("15 09 * * 0");
    });

    test("falls back to daily when no weekly day is selected", () => {
      const cron = buildCron({ frequency: "weekly", daysOfWeek: [], time: "08:30" });
      expect(cron).toBe("30 08 * * *");
    });

    test("builds a monthly cron on a given day of month", () => {
      const cron = buildCron({ frequency: "monthly", dayOfMonth: "15", time: "17:30" });
      expect(cron).toBe("30 17 15 * *");
    });

    test("clamps invalid time values", () => {
      expect(buildCron({ time: "25:70" })).toBe("59 23 * * *");
      expect(buildCron({ time: "8:5" })).toBe("05 08 * * *");
    });

    test("clamps invalid day of month", () => {
      expect(buildCron({ frequency: "monthly", dayOfMonth: "40" })).toBe("30 08 31 * *");
      expect(buildCron({ frequency: "monthly", dayOfMonth: "0" })).toBe("30 08 1 * *");
    });
  });

  describe("isValidCron", () => {
    test("accepts a standard 5-field cron", () => {
      expect(isValidCron("30 8 * * *")).toBe(true);
    });

    test("accepts a 6-field cron", () => {
      expect(isValidCron("30 8 * * * 1-5")).toBe(true);
    });

    test("accepts lists and steps", () => {
      expect(isValidCron("0 8,13,17 * * 1-4")).toBe(true);
      expect(isValidCron("*/15 * * * *")).toBe(true);
    });

    test("rejects malformed expressions", () => {
      expect(isValidCron("")).toBe(false);
      expect(isValidCron("30 8 * *")).toBe(false);
      expect(isValidCron("30 8 * * * * *")).toBe(false);
      expect(isValidCron("abc def * * *")).toBe(false);
      expect(isValidCron(42)).toBe(false);
      expect(isValidCron(null)).toBe(false);
    });
  });

  describe("describeCron", () => {
    test("describes a daily schedule", () => {
      expect(describeCron("30 8 * * *")).toBe("Todos los días a las 08:30");
    });

    test("describes a weekly schedule by weekday names", () => {
      expect(describeCron("30 13 * * 1,4,5")).toBe("lunes, jueves, viernes a las 13:30");
    });

    test("describes a sunday schedule", () => {
      expect(describeCron("15 9 * * 0")).toBe("domingo a las 09:15");
    });

    test("describes a monthly schedule", () => {
      expect(describeCron("30 17 15 * *")).toBe("Día 15 de cada mes, a las 17:30");
    });

    test("describes a 6-field cron the same way", () => {
      expect(describeCron("0 30 8 * * 1-5")).toBe("lunes, martes, miércoles, jueves, viernes a las 08:30");
    });

    test("handles ranges in the day-of-week field", () => {
      expect(describeCron("0 8 * * 1-5")).toBe("lunes, martes, miércoles, jueves, viernes a las 08:00");
    });

    test("returns an error label for invalid cron", () => {
      expect(describeCron("not a cron")).toBe("Expresión cron inválida");
    });
  });
});
