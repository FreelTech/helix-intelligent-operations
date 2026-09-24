import globals from "globals";

export default [
    {
        files: ["js/**/*.js"],
        languageOptions: {
            ecmaVersion: 2018,
            sourceType: "script",
            globals: {
                ...globals.browser,
                Xrm: "readonly"
            }
        },
        rules: {
            "no-undef": "error",
            "no-unused-vars": "warn",
            "eqeqeq": ["error", "always"],
            "no-restricted-properties": [
                "error",
                {
                    "object": "Xrm",
                    "property": "Page",
                    "message": "Xrm.Page is deprecated. Use the execution context and getFormContext()."
                }
            ]
        }
    }
];