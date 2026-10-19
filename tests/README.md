# Testing

The project contains two test groups:

- **EditMode tests:** check project configuration, build scenes, and missing scripts in prefabs.
- **PlayMode tests:** load the first build scene and check that it loads correctly, has a `MainCamera`, and produces no unexpected errors.

## Run manually

1. Open the project in Unity.
2. Open **Window > General > Test Runner**.
3. Run the **EditMode** tests.
4. Run the **PlayMode** tests.

Before running PlayMode tests, add the main scene to **File > Build Profiles** and tag its active camera as `MainCamera`.

## Run automatically

GitHub Actions runs both test groups on pushes and pull requests to `main` using the workflow once enabled
