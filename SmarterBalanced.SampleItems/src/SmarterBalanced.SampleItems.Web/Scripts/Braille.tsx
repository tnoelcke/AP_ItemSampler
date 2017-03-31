﻿namespace Braille {

    export interface Props {
        currentSelectionCode: string;
        brailleItemCodes: string[];
        braillePassageCodes: string[];
        bankKey: number;
        itemKey: number;
    }

    export class BrailleLink extends React.Component<Props, {}> {
        constructor(props: Props) {
            super(props);
        }

        buildUrl(bankKey: number, itemKey: number, ): string {
            var brailleType = "";
            if (typeof (this.props.brailleItemCodes) != 'undefined' && this.props.brailleItemCodes.indexOf(this.props.currentSelectionCode) > -1) {
               var brailleLoc = this.props.brailleItemCodes.indexOf(this.props.currentSelectionCode);
               brailleType = this.props.brailleItemCodes[brailleLoc];
            }
            var url = "/Item/Braille?bankKey=" + bankKey + "&itemKey=" + itemKey + "/" + brailleType;
            return url;
        }

        render() {
            let brailleUrl = this.buildUrl(this.props.bankKey, this.props.itemKey);
            return (
                <a> {brailleUrl} </a>
            );
        }
    }
}