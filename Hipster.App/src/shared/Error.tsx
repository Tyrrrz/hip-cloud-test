function resolveError(error: unknown): string | null {
  if (!error) {
    return null;
  }

  if (typeof error === "string") {
    return error;
  }

  if (typeof error === "object") {
    const errorRecord = error as Record<string, unknown>;

    if (errorRecord.message && typeof errorRecord.message === "string") {
      return errorRecord.message;
    }

    if (errorRecord.detail && typeof errorRecord.detail === "string") {
      return errorRecord.detail;
    }

    if (errorRecord.title && typeof errorRecord.title === "string") {
      return errorRecord.title;
    }

    return errorRecord.toString();
  }

  return null;
}

interface ErrorProps {
  error: unknown;
}

export default function Error({ error }: ErrorProps) {
  const resolvedError = resolveError(error);
  if (!resolvedError) {
    return <></>;
  }

  return <span>{resolvedError}</span>;
}
