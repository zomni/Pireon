export const DAYS_OF_WEEK = ["lu", "ma", "mi", "ju", "vi", "sa", "do"];

export const WEEKDAY_LABELS = {
  lu: "Lunes",
  ma: "Martes",
  mi: "Miércoles",
  ju: "Jueves",
  vi: "Viernes",
  sa: "Sábado",
  do: "Domingo",
};

const WEEKDAY_INDEX = {
  lu: 1,
  ma: 2,
  mi: 3,
  ju: 4,
  vi: 5,
  sa: 6,
  do: 0,
};

const DOW_LABELS = {
  0: "domingo",
  1: "lunes",
  2: "martes",
  3: "miércoles",
  4: "jueves",
  5: "viernes",
  6: "sábado",
};

const pad = (value) => String(value).padStart(2, "0");

const normalizeTime = (time) => {
  const parts = String(time || "08:30").split(":");
  const hour = Math.max(0, Math.min(23, parseInt(parts[0], 10) || 0));
  const minute = Math.max(0, Math.min(59, parseInt(parts[1], 10) || 0));
  return { hour, minute };
};

export const buildCron = ({ frequency = "daily", daysOfWeek = [], dayOfMonth = 1, time = "08:30" } = {}) => {
  const { hour, minute } = normalizeTime(time);
  const mm = pad(minute);
  const hh = pad(hour);

  if (frequency === "monthly") {
    const dom = Math.max(1, Math.min(31, parseInt(dayOfMonth, 10) || 1));
    return `${mm} ${hh} ${dom} * *`;
  }

  if (frequency === "weekly") {
    const days = daysOfWeek
      .filter((day) => WEEKDAY_INDEX[day] !== undefined)
      .map((day) => WEEKDAY_INDEX[day])
      .sort((a, b) => a - b);
    if (days.length === 0) {
      return `${mm} ${hh} * * *`;
    }
    return `${mm} ${hh} * * ${days.join(",")}`;
  }

  return `${mm} ${hh} * * *`;
};

const SIMPLE_FIELD_RE = /^(\*|(\d+)((,\d+)*)|(\d+-\d+)|(\*\/\d+))$/;

export const isValidCron = (cron) => {
  if (typeof cron !== "string") return false;
  const fields = cron.trim().split(/\s+/);
  if (fields.length !== 5 && fields.length !== 6) return false;
  return fields.every((field) => SIMPLE_FIELD_RE.test(field));
};

const expandField = (value) => {
  const token = String(value).trim();
  if (token === "*") return null;
  if (token.includes("-")) {
    const [from, to] = token.split("-").map((part) => parseInt(part, 10));
    if (!Number.isInteger(from) || !Number.isInteger(to) || from > to) return null;
    return Array.from({ length: to - from + 1 }, (_, i) => from + i);
  }
  if (token.includes(",")) {
    const values = token.split(",").map((part) => parseInt(part, 10));
    return values.every((part) => Number.isInteger(part)) ? values : null;
  }
  const valueNumber = parseInt(token, 10);
  return Number.isInteger(valueNumber) ? [valueNumber] : null;
};

export const describeCron = (cron) => {
  if (!isValidCron(cron)) {
    return "Expresión cron inválida";
  }

  const fields = cron.trim().split(/\s+/);
  let minute;
  let hour;
  let dom;
  let month;
  let dow;

  if (fields.length === 6) {
    [, minute, hour, dom, month, dow] = fields;
  } else {
    [minute, hour, dom, month, dow] = fields;
  }

  const minuteNum = parseInt(minute, 10);
  const hourNum = parseInt(hour, 10);
  const time =
    Number.isInteger(hourNum) && Number.isInteger(minuteNum)
      ? `${pad(hourNum)}:${pad(minuteNum)}`
      : null;
  const atTime = time ? `a las ${time}` : "cada hora";

  if (dom !== "*" && dom !== "?") {
    const days = expandField(dom);
    if (days && days.length === 1) {
      return `Día ${days[0]} de cada mes, ${atTime}`;
    }
    return `Días ${dom} de cada mes, ${atTime}`;
  }

  if (dow !== "*" && dow !== "?") {
    const days = expandField(dow);
    if (days) {
      const labels = days.map((day) => DOW_LABELS[day] ?? day).join(", ");
      return `${labels} ${atTime}`;
    }
  }

  return `Todos los días ${atTime}`;
};
