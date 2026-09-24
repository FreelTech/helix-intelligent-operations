"use strict";

var Helix = window.Helix || {};
Helix.Case = Helix.Case || {};

(function () {

    // -----------------------------------------------------------------
    // CONFIGURATION
    // Every value below must be READ FROM THE ENVIRONMENT, not copied
    // from the file that gave you this template. A wrong table name
    // throws loudly; a wrong column name returns undefined and this
    // script silently decides nobody speaks Welsh.
    // -----------------------------------------------------------------
    var NAMES = {
        contactLookup:   "hlx_helixcontact",            // the LOOKUP COLUMN on hlx_case
        contactTable:    "hlx_helixcontact",       // the TABLE it points at
        languageColumn:  "hlx_languageperference", // on the contact
        welshValue:      740000001,                 // <-- READ THIS FROM THE CHOICE
        tabName:         "general_tab",                  // <-- READ THIS FROM THE FORM
        sectionName:     "sec_welsh",
        welshSummary:    "hlx_summarycy"
    };

    var WARNING_ID = "helix_welsh_lookup_failed";

    // --- small helpers, each one null-safe on purpose ----------------

    function getWelshSection(formContext) {
        var tab = formContext.ui.tabs.get(NAMES.tabName);
        if (tab === null) {
            console.warn("caseWelsh: no tab named " + NAMES.tabName);
            return null;
        }
        var section = tab.sections.get(NAMES.sectionName);
        if (section === null) {
            console.warn("caseWelsh: no section named " + NAMES.sectionName);
        }
        return section;
    }

    function setWelshVisible(formContext, visible) {
        var section = getWelshSection(formContext);
        if (section !== null) {
            section.setVisible(visible);
        }

        var control = formContext.getControl(NAMES.welshSummary);
        if (control !== null) {
            control.setVisible(visible);
        } else {
            console.warn("caseWelsh: " + NAMES.welshSummary + " is not on this form");
        }
    }

    // --- the actual decision ----------------------------------------

    function applyForContact(formContext) {
        var attribute = formContext.getAttribute(NAMES.contactLookup);
        if (attribute === null) {
            console.warn("caseWelsh: " + NAMES.contactLookup + " is not on this form");
            return;
        }

        var value = attribute.getValue();
        if (value === null || value.length === 0) {
            setWelshVisible(formContext, false);
            formContext.ui.clearFormNotification(WARNING_ID);
            return;
        }

        // A lookup returns an ARRAY, and the id arrives wrapped in braces.
        var id = value[0].id.replace("{", "").replace("}", "");

        Xrm.WebApi.retrieveRecord(
            NAMES.contactTable,
            id,
            "?$select=" + NAMES.languageColumn
        ).then(
            function (record) {
                var preference = record[NAMES.languageColumn];
                setWelshVisible(formContext, preference === NAMES.welshValue);
                formContext.ui.clearFormNotification(WARNING_ID);
            },
            function (error) {
                // Fail CLOSED: if we cannot confirm a Welsh preference,
                // do not present a Welsh service, and say why.
                setWelshVisible(formContext, false);
                formContext.ui.setFormNotification(
                    "Could not read the contact's language preference, so Welsh " +
                    "fields are hidden. " + error.message,
                    "WARNING",
                    WARNING_ID
                );
            }
        );
    }

    // --- the two entry points ---------------------------------------

    this.onLoad = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var attribute = formContext.getAttribute(NAMES.contactLookup);
        if (attribute !== null) {
            attribute.addOnChange(Helix.Case.onContactChange);
        }

        applyForContact(formContext);
    };

    this.onContactChange = function (executionContext) {
        applyForContact(executionContext.getFormContext());
    };

}).call(Helix.Case);