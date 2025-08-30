import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Pagination from '@/components/ui/pagination';
//components
import AddDomain from './components/add';
//context
import { useSession } from '@/context/session';
//api
import { Domains } from '@/api/user/domains';
import { Languages } from '@/api/user/languages';
//helpers
// No longer using string-based sorting helpers
import { printDate } from '@/helpers/datetime';

/**
 * <summary>Admin Domains List Page</summary>
 * <description>Displays and manages the list of domains in the admin panel.</description>
 */
export default function AdminDomains() {
    const navigate = useNavigate();
    const session = useSession();
    const { getDomains, getDomainTypes } = Domains(session);
    const { getAll } = Languages(session);

    const [domains, setDomains] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [debounceTimer, setDebounceTimer] = useState(null);
    const [domainTypes, setDomainTypes] = useState([]);
    const [languages, setLanguages] = useState([]);

    // Create a filter object with all filter parameters including pagination
    const [filter, setFilter] = useState({
        search: '',
        sort: 0, // 0 = domain ASC
        subjectIds: [],
        type: 0, // DomainFilterType.All
        domainType: -1,
        domainType2: -1,
        lang: '',
        start: 0,
        length: 50,
        parentId: -1
    });

    const [totalItems, setTotalItems] = useState(0);
    const [totalPages, setTotalPages] = useState(1);

    useEffect(() => {
        if (domains.length === 0) {
            filterDomains();
        }
        getDomainTypes().then(response => {
            if (response.data.success) {
                setDomainTypes(response.data.data);
            }
        });
        getAll().then(response => {
            if (response.data.success) {
                // Convert dictionary to array of objects
                const languageData = response.data.data;
                const languageOptions = Object.keys(languageData).map(code => ({
                    value: code,
                    label: languageData[code]
                }));
                setLanguages(languageOptions);
            }
        }).catch(error => {
            console.error('Error fetching languages:', error);
        });
    }, []);

    const filterDomains = (customFilter) => {
        // Use the custom filter if provided, otherwise use the state filter
        const requestFilter = customFilter || filter;

        getDomains({
            Search: requestFilter.search || '',
            Sort: requestFilter.sort, // Using integer value directly: 0 = ASC, 1 = DESC, 2 = most articles, 3 = newest, 4 = oldest, 5 = last updated
            SubjectIds: requestFilter.subjectIds,
            Type: requestFilter.type, // DomainFilterType.All
            DomainType: requestFilter.domainType, // Default value in stored procedure
            DomainType2: requestFilter.domainType2, // Default value in stored procedure
            Lang: requestFilter.lang,
            Start: requestFilter.start,
            Length: requestFilter.length,
            ParentId: requestFilter.parentId
        }).then(response => {
            if (response.data.success) {
                setDomains(response.data.data.domains || []);
                setTotalItems(response.data.data.totalCount || 0);
                setTotalPages(Math.ceil((response.data.data.totalCount || 0) / filter.length));
            }
        }).catch(error => {
            console.error('Error fetching domains:', error);
        });
    };

    const handlePageChange = (page) => {
        const start = (page - 1) * filter.length;
        const newFilter = { ...filter, start };
        setFilter(newFilter);
        filterDomains(newFilter);
    };

    // Handle sorting for all columns with a single method
    const handleSort = (primarySort, secondarySort) => {
        setFilter(prev => ({
            ...prev,
            sort: prev.sort === primarySort ? secondarySort : primarySort
        }));
    };

    const handleDomainNameInput = (e) => {
        const inputValue = e.target.value;

        setFilter(prev => ({
            ...prev,
            search: inputValue
        }));

        // Clear previous timer if exists
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }

        // Create a new filter object with the updated search value
        const newFilter = {
            ...filter,
            search: inputValue
        };

        // Set new timer and pass the new filter object to the function
        const timer = setTimeout(() => {
            filterDomains(newFilter);
        }, 500);

        setDebounceTimer(timer);
    };

    const handleFilterChange = (field, value) => {
        setFilter(prev => ({
            ...prev,
            [field]: value
        }));

        // Create a new filter object with the updated field value
        const newFilter = {
            ...filter,
            [field]: value
        };

        // Only apply debouncing to text fields (search and lang)
        if (field === 'search' || field === 'lang') {
            // Clear any existing timer
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            // Set a new timer and pass the new filter object to the function
            const timer = setTimeout(() => {
                filterDomains(newFilter);
            }, 500);

            setDebounceTimer(timer);
        } else {
            // For non-text fields, fetch immediately
            filterDomains(newFilter);
        }
    };

    const handleClosedAddDomain = (domain) => {
        if (domain) {
            filterDomains();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Domain</button>
    </>);

    return (
        <div className="admin-domains">
            {showAdd && <AddDomain onClose={handleClosedAddDomain}></AddDomain>}
            <Container
                title="Domain Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="domainsearch"
                        type="text"
                        placeholder="Search by Domain Name"
                        value={filter.search}
                        onInput={handleDomainNameInput}
                        className="domainNameInput"
                    />
                    <Select
                        name="domainType"
                        value={filter.domainType}
                        onChange={(e) => handleFilterChange('domainType', parseInt(e.target.value))}
                        options={domainTypes
                            .sort((a, b) => {
                                // Ensure 'All' (value -1) is at the top
                                if (a.value === -1) return -1;
                                if (b.value === -1) return 1;
                                return a.label.localeCompare(b.label);
                            })}
                    />
                    <Select
                        name="domainType2"
                        value={filter.domainType2}
                        onChange={(e) => handleFilterChange('domainType2', parseInt(e.target.value))}
                        options={domainTypes
                            .sort((a, b) => {
                                // Ensure 'All' (value -1) is at the top
                                if (a.value === -1) return -1;
                                if (b.value === -1) return 1;
                                return a.label.localeCompare(b.label);
                            })}
                    />
                    <Select
                        name="lang"
                        value={filter.lang}
                        onChange={(e) => handleFilterChange('lang', e.target.value)}
                        options={[
                            { value: '', label: 'All Languages' },
                            ...languages
                        ]}
                    />
                    <Select
                        name="type"
                        value={filter.type}
                        onChange={(e) => handleFilterChange('type', parseInt(e.target.value))}
                        options={[
                            { value: 0, label: 'All' },
                            { value: 1, label: 'Active' },
                            { value: 2, label: 'Inactive' }
                        ]}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => handleSort(0, 1)}>
                                Domain {filter.sort === 0 && <Icon name="arrow_upward" />}
                                {filter.sort === 1 && <Icon name="arrow_downward" />}
                            </th>
                            <th onClick={() => handleSort(6, 7)}>
                                Title {filter.sort === 6 && <Icon name="arrow_upward" />}
                                {filter.sort === 7 && <Icon name="arrow_downward" />}
                            </th>
                            <th onClick={() => handleSort(2, 8)}>
                                Articles {filter.sort === 2 && <Icon name="arrow_downward" />}
                                {filter.sort === 8 && <Icon name="arrow_upward" />}
                            </th>
                            <th onClick={() => handleSort(3, 4)}>
                                Created {filter.sort === 3 && <Icon name="arrow_downward" />}
                                {filter.sort === 4 && <Icon name="arrow_upward" />}
                            </th>
                            <th onClick={() => handleSort(5, 9)}>
                                Updated {filter.sort === 5 && <Icon name="arrow_downward" />}
                                {filter.sort === 9 && <Icon name="arrow_upward" />}
                            </th>
                            <th onClick={() => handleSort(10, 11)}>
                                Status {filter.sort === 10 && <Icon name="arrow_upward" />}
                                {filter.sort === 11 && <Icon name="arrow_downward" />}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {domains.map(domain =>
                            <tr
                                key={domain.domainId}
                                onClick={() => { }}
                            >
                                <td>{domain.domain}</td>
                                <td>{domain.title || domain.domain}</td>
                                <td>{domain.articles || 0}</td>
                                <td>{domain.created ? printDate(domain.created) : 'Unknown'}</td>
                                <td>{domain.lastchecked ? printDate(domain.lastchecked) : 'Never'}</td>
                                <td>
                                    {domain.deleted ? 'Deleted' :
                                    domain.blacklisted ? 'Blacklisted' :
                                    domain.whitelisted ? 'Whitelisted' :
                                    domain.empty ? 'Empty' : 'Active'}
                                </td>
                                <td className="buttons">
                                    <Link to={'/admin/domains/edit/' + domain.domainId} title="edit domain"><Icon name="edit_square"></Icon></Link>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>

                <Pagination
                    currentPage={Math.floor(filter.start / filter.length) + 1}
                    totalPages={totalPages}
                    pageSize={filter.length}
                    totalItems={totalItems}
                    onPageChange={handlePageChange}
                    maxPageNumbers={9}
                />
            </Container>
        </div>
    );
}
