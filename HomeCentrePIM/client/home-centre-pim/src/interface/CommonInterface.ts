export interface IDatePicker {
    startDate?: Date | undefined;
    endDate?: Date | undefined;
}

export interface IDetailsListBasicExampleItem {
    id: number;
    name: string;
    description: string;
}

export interface TabPanelProps {
    children?: React.ReactNode;
    index: number;
    value: number;
}

export interface IDetailsListBasicExampleState {
    items: IDetailsListBasicExampleItem[];
    selectionDetails: string;
    statusOfRecords: boolean;
    editingRecordId: boolean;
    visible: boolean;
    actDeactID: boolean;
    records: [];
    value: string;
    pageNo: number;
    cpyRecord: [];
    appuserid: Number;
    postsPerPage: any;
    currentPage: any;
    roledata: [];
    countState: boolean;
    searchKey: any;
    roleList: any;
    rvisible: any;
    userRoleId: any;
    selectedKeys: string[];
}







