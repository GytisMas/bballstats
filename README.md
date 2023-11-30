# bballstats
Sports (basketball) statistics web application. Features include user authentication / authorisation, statistics management (based on user role) of basketball teams / players.
Unique feature (WIP): users can create their own rating systems for players by writing mathematical formulas that use specific statistic types. Users can find those rating 'algorithms' and see how players are ranked by them.
Currently features only a back-end API which is accessible from https://whale-app-wxvqi.ondigitalocean.app/.

API
Users can access and manage some data from the sports database. Their level of control depends on the user role:
- All (un)authorised users can:
  - Get info of specific user;
  - Get info of all or specific rating algorithms and each entity relating to the sports database.
- Regular users can:
  - Create rating algorithms;
  - Give an impression (basically like / dislike) on rating algorithms of other users.
- Curators can:
  - Manage statistic types for players (CRUD) and manage their visibility ('Statistic' entity).
- Moderators can:
  - Manage the sports statistics database ('Team', 'Player', and 'PlayerStatistic' entities).
- Admins can:
  - Manage the user database.
The API endpoints are written in the Endpoints.cs file.

Authentication
User class is derived from Microsoft.AspNetCore.Identity's IdentityUser. Upon successful registration, a new user is created using Microsoft.AspNetCore.Identity's UserManager.
User authentication is implemented using JWT. the JWT token's contained information includes not only the user, but also the user's roles.
Upon user login, two JWT tokens are generated: 
- Access token, which lasts for 10 minutes;
- Refresh token, which lasts for 24 hours.
The 'accessToken' API call is available for creating new access and refresh tokens.
The user class contains an extra parameter called 'ForceRelogin' which is set as false upon logging in and set as true upon logging out. This parameter being set as true renders the user's refresh token invalid.
User authentication API endpoints are written in the AuthEndpoints.cs file which is located in the 'Auth' folder.

Authorisation
Users should access limited data depending on their role. The role of the user specified in the JWT token is compared to the one that is required. This is done with 'if' statements written in the code of each API call. An authorisation handler that specifies the role may also be used to forbid entry without an access token.
