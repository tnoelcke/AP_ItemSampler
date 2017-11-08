﻿import '../Styles/search.less';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import * as ItemCard from '../ItemCard/ItemCard';
import * as ItemCardModels from '../ItemCard/ItemCardModels';
import * as ItemsSearchParams from './ItemsSearchParams';
import * as GradeLevels from '../GradeLevels/GradeLevels';
import * as Models from './ItemsSearchModels';
import { Resource, get } from '../ApiModel';
import * as ItemSearchModels from './ItemsSearchModels';
import { RouteComponentProps } from 'react-router';
import { AdvancedFilterCategory, AdvancedFilterContainer, AdvancedFilterOption } from '@osu-cass/react-advanced-filter';
import { mockAdvancedFilterCategories } from './filterModels';


export const ItemsSearchClient = (params: ItemSearchModels.SearchAPIParams) =>
    get<ItemCardModels.ItemCardViewModel[]>("/BrowseItems/search", params);

export const ItemsViewModelClient = () =>
    get<ItemSearchModels.ItemsSearchViewModel>("/BrowseItems/ItemsSearchViewModel");


export interface Props extends RouteComponentProps<{}> {
    itemsSearchClient: (params: Models.SearchAPIParams) => Promise<ItemCardModels.ItemCardViewModel[]>;
    itemsViewModelClient: () => Promise<ItemSearchModels.ItemsSearchViewModel>;
}

export interface State {
    searchResults: Resource<ItemCardModels.ItemCardViewModel[]>;
    itemSearch: Resource<ItemSearchModels.ItemsSearchViewModel>;
    currentFilter: AdvancedFilterCategory[];
}

export class ItemsSearchComponent extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = {
            searchResults: { kind: "loading" },
            itemSearch: { kind: "loading" },
            currentFilter: mockAdvancedFilterCategories
        };

        this.props.itemsViewModelClient()
            .then(data => this.onFetchedItemSearch(data))
            .catch(err => this.onError(err));
        
    }

    beginSearch(params: Models.SearchAPIParams) {
        const searchResults = this.state.searchResults;
        if (searchResults.kind === "success") {
            this.setState({
                searchResults: {
                    kind: "reloading",
                    content: searchResults.content
                }
            });
        } else if (searchResults.kind === "failure") {
            this.setState({
                searchResults: { kind: "loading" }
            });
        }

        this.props.itemsSearchClient(params)
            .then((data) => this.onSearch(data))
            .catch((err) => this.onError(err));
    }

    onSearch(results: ItemCardModels.ItemCardViewModel[]) {
        this.setState({ searchResults: { kind: "success", content: results } });
    }

    onError(err: any) {
        this.setState({ searchResults: { kind: "failure" } });
    }

    onFetchedItemSearch(itemsSearch: ItemSearchModels.ItemsSearchViewModel) {
        const newFilters = [...this.state.currentFilter];

        const newGrade = this.state.currentFilter.find(f => f.label.toLocaleLowerCase() === "grades")
        const newSubject = this.state.currentFilter.find(f => f.label.toLocaleLowerCase() === "subjects");
        if (newGrade && newSubject) {
            // TODO: get buisness logic for displable filter options. 
            //if math is selected change displayed claims/targets/etc...

            //else if english is selected change displayed  claims/targets/etc...
        }





        this.setState({
            itemSearch: { kind: "success", content: itemsSearch },
            currentFilter: newFilters
        }, );

    }

    updateCurrentFilterOnLoad(itemsSearch: ItemSearchModels.ItemsSearchViewModel) {
        this.setState({})
    }

    selectSingleResult() {
        const searchResults = this.state.searchResults;
        if (searchResults.kind === "success" && searchResults.content && searchResults.content.length === 1) {
            const searchResult = searchResults.content[0];
            ItemCardModels.itemPageLink(searchResult.bankKey, searchResult.itemKey);
        }
    }

    isLoading() {
        return this.state.searchResults.kind === "loading" || this.state.searchResults.kind === "reloading";
    }

    renderResultElement(): JSX.Element | JSX.Element[] | undefined {
        const searchResults = this.state.searchResults;

        let resultsElement: JSX.Element[] | JSX.Element | undefined;
        if ((searchResults.kind === "success" || searchResults.kind === "reloading") && searchResults.content) {
            resultsElement = searchResults.content && searchResults.content.length === 0
                ? <span className="placeholder-text" role="alert">No results found for the given search terms.</span>
                : searchResults.content.map(digest =>
                    <ItemCard.ItemCard {...digest} key={digest.bankKey.toString() + "-" + digest.itemKey.toString()} />);
        } else if (searchResults.kind === "failure") {
            resultsElement = <div className="placeholder-text" role="alert">An error occurred. Please try again later.</div>;
        } else {
            resultsElement = undefined;
        }
        return resultsElement;
    }

    renderISPComponent(): JSX.Element {
        const isLoading = this.isLoading();
        const searchVm = this.state.itemSearch;

        if (searchVm.kind == "success" || searchVm.kind == "reloading") {
            if (searchVm.content) {
                return (
                    <ItemsSearchParams.ISPComponent
                        interactionTypes={searchVm.content.interactionTypes}
                        subjects={searchVm.content.subjects}
                        onChange={(params) => this.beginSearch(params)}
                        selectSingleResult={() => this.selectSingleResult()}
                        isLoading={isLoading} />
                );
            }
            else {
               return <p><em>Loading...</em></p>
            }
        }
        else {
            return <p><em>Loading...</em></p>
        }

       
    }


    // TODO: Optimize this 
    translateAdvancedFilterCate(categorys: AdvancedFilterCategory[]): Models.SearchAPIParams {
        let model: Models.SearchAPIParams = {
            itemId: "",
            gradeLevels: GradeLevels.GradeLevels.All,
            subjects: [],
            claims: [],
            interactionTypes: [],
            performanceOnly: false,
            targets:[]
        };

        const gradeCategory = categorys.find(c => c.label.toLowerCase() === 'grades');
        if (gradeCategory && !gradeCategory.disabled) {
            model.gradeLevels = GradeLevels.GradeLevels.All;
            const selectedGrade = gradeCategory.filterOptions.find(fo => fo.isSelected);
            if (selectedGrade) {
                model.gradeLevels = Number(selectedGrade.key);
            }
        }

        const subjectsCategory = categorys.find(c => c.label.toLowerCase() === 'subjects');
        if (subjectsCategory && !subjectsCategory.disabled) {
            subjectsCategory.filterOptions.forEach(fo => {
                if (fo.isSelected) {
                    model.subjects.push(fo.key);
                }
            });
        }

        //look up techtype options
        const techTypesCategory = categorys.find(c => c.label.toLowerCase() === 'TechType');
        if (techTypesCategory && !techTypesCategory.disabled) {
            const selectedTechType = techTypesCategory.filterOptions.find(fo => fo.key.toLowerCase() === 'pt');
            if (selectedTechType) {
                model.performanceOnly = selectedTechType.isSelected;
            }
        }

        return model;
    }

    renderAdvancedFilter() {
        if (this.state.itemSearch.kind === "success") {
            //subjects to display
            const subjects = this.state.itemSearch.content!.subjects;

            //claims to display
            const subjectCategory = this.state.currentFilter
                .find(f => f.label === "Subjects");
            const selectedSubjectOptions = (subjectCategory ? subjectCategory.filterOptions : [])
                .filter(fo => fo.isSelected);
            const selectedSubjects = subjects.filter(s => selectedSubjectOptions.findIndex(ss => ss.key === s.code) !== -1);
            const claimsToDisplay = selectedSubjects
                .map(s => s.claims)
                .reduce((prev, curr) => prev.concat(curr), []);

            //targets to display
            const claimCategory = this.state.currentFilter
                .find(f => f.label === "Claims");
            const selectedClaimOptions = (claimCategory ? claimCategory.filterOptions : [])
                .filter(fo => fo.isSelected);
            const selectedClaims = claimsToDisplay.filter(c => selectedClaimOptions.findIndex(sco => sco.key === c.code) !== -1);
            const targetsToDisplay = selectedClaims.map(c => c.targets).reduce((prev, curr) => prev.concat(curr), []);
        }
    }

    beginSearchFilter = (categories: AdvancedFilterCategory[]) => {
        this.setState({
            currentFilter: categories
        });
        const searchResults = this.state.searchResults;
        if (searchResults.kind === "success") {
            this.setState({
                searchResults: {
                    kind: "reloading",
                    content: searchResults.content
                }
            });
        } else if (searchResults.kind === "failure") {
            this.setState({
                searchResults: { kind: "loading" }
            });
        }

        const params = this.translateAdvancedFilterCate(categories);

        this.props.itemsSearchClient(params)
            .then((data) => this.onSearch(data))
            .catch((err) => this.onError(err));
    }


    renderfilters() {
        const isLoading = this.isLoading();
        const searchVm = this.state.itemSearch;

        const param = this.translateAdvancedFilterCate(this.state.currentFilter);

        //pull item cards
        this.props.itemsSearchClient(param)
            .then((data) => this.onSearch(data))
            .catch((err) => this.onError(err));


        if (searchVm.kind == "success" || searchVm.kind == "reloading") {
            if (searchVm.content) {
                return (
                    <AdvancedFilterContainer filterOptions={...this.state.currentFilter} onClick={this.beginSearchFilter} />
                );
            }
            else {
                return <p><em>Loading...</em></p>
            }
        }
        else {
            return <p><em>Loading...</em></p>
        }
    }

    render() {
        return (
            <div className="search-container" style={{ "backgroundColor": "white", "marginTop":"50px"}}>
                {this.renderfilters()}
                <div className="search-results" >
                    {this.renderResultElement()}
                </div>
            </div>
        );
    }
}
