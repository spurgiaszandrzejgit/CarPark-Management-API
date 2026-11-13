import { HttpErrorResponse } from '@angular/common/http';

export function getApiErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Unable to connect to the server.';
  }

  if (error.status === 409) {
    return 'The operation conflicts with the current parking state.';
  }

  if (error.status === 400) {
    return 'Invalid request. Please check the vehicle registration.';
  }

  if (error.status >= 500) {
    return 'A server error occurred. Please try again later.';
  }

  return 'Unable to process the request.';
}