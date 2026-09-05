# Ecommerce - Applied Fixes

- Stripe PaymentIntent creation now sends the IdempotencyKey.
- Product create/update/delete/image endpoints require the Admin role.
- Wishlist uses a filtered unique index so soft-deleted rows do not block re-adding.
- Wishlist queries explicitly exclude deleted records and handle duplicate inserts safely.
- Account GET no longer rotates refresh tokens.
- Refresh-token rotation is kept in the refresh endpoint; stale tokens are cleaned up.
- Refresh-token revoke is scoped to the authenticated user.
- Basket endpoints are authenticated and use the current user id as the Redis key.
- Payment endpoint no longer accepts an arbitrary basket id.
- Order creation requires a PaymentIntent and is idempotent by PaymentIntentId instead of deleting an existing order.
- Payment failure restores stock once when the order transitions to PaymentFailed.
- Product image replacement cleans up the previous local image after a successful DB update.
- Removed WeatherForecast template files and generated build/log artifacts.
- Added a migration for the Wishlist filtered unique index.

## Important
Run `dotnet restore`, `dotnet build`, and `dotnet test` locally before `dotnet ef database update`.
The new migration must be applied to the ApplicationDbContext database.
