import { User, UserPermissions } from "nhitomi-api";

export function userIsAdmin(user: User) {
  return user.permissions.includes(UserPermissions.Administrator);
}

export function userHasPermissions(user: User, ...flags: UserPermissions[]) {
  if (userIsAdmin(user)) {
    return true;
  }

  for (const flag of flags) {
    if (!user.permissions.includes(flag)) {
      return false;
    }
  }

  return true;
}

export function userHasAnyPermission(user: User, ...flags: UserPermissions[]) {
  if (userIsAdmin(user)) {
    return true;
  }

  for (const flag of flags) {
    if (user.permissions.includes(flag)) {
      return true;
    }
  }

  return false;
}
