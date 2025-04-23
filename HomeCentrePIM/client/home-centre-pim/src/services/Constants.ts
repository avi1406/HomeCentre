import { t } from "i18next";

export const baseUrl = window.location.origin.includes('localhost:') ? 'https://localhost:5001' : API_ENDPOINT;

export const Constants = {
  Warning: {
    InvalidPhoneNumberWarning: 'Phone No must be between 7 and 12 numbers long!',
  },
};

export const fieldType: any = [
  { key: true, text: t('MANDATORY') },
  { key: false, text: t('OPTIONAL') },
];

export const result = [{ "PlanoOrFamilyname": "pot", "Division": "Household", "Type": "Planogram", "Description": "pot is a planogram and the name that in the application and it is stored in it", "CreatedBy": "Gokul", "DateCreated": "11/09/2022", "ApproverL1": "Pradeep", "ActionL1": "Approved", "DateofActionbyL1": "11 / 09 / 2022 ", "ApproverL2": "jack", "ActionL2": "Approved", "DateofActionbyL2": "19/09/22" },
{ "PlanoOrFamilyname": "pen", "Division": "Household", "Type": "Planogram", "Description": "pen is a planogram and the name that in the application and it is stored in it", "CreatedBy": "deepak", "DateCreated": "14/09/2022", "ApproverL1": "Pradeep", "ActionL1": "Approved", "DateofActionbyL1": "14 / 09 / 2022 ", "ApproverL2": "max", "ActionL2": "Pending", "DateofActionbyL2": "19/09/22" },
{ "PlanoOrFamilyname": "pot", "Division": "Household", "Type": "Planogram", "Description": "pot is a planogram and the name that in the application and it is stored in it", "CreatedBy": "Gokul", "DateCreated": "11/09/2022", "ApproverL1": "Pradeep", "ActionL1": "Approved", "DateofActionbyL1": "11 / 09 / 2022 ", "ApproverL2": "jack", "ActionL2": "Approved", "DateofActionbyL2": "19/09/22" },
{ "PlanoOrFamilyname": "B00k", "Division": "Furniture", "Type": "Planogram", "Description": "Book is a planogram and the name that in the application and it is stored in it", "CreatedBy": "Sakthivelan", "DateCreated": "10/09/2022", "ApproverL1": "Rajesh", "ActionL1": "Rejected", "DateofActionbyL1": "1 / 09 / 2022 ", "ApproverL2": "jack", "ActionL2": "Approved", "DateofActionbyL2": "20/09/22" }];