import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
//context
import { useSession } from '@/context/session';
//api
import { Articles } from '@/api/user/articles';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import { localDateTime, printDate } from '@/helpers/datetime';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Articles List Page</summary>
 * <description>Displays and manages the list of articles in the admin panel.</description>
 */
export default function AdminArticles() {
    const navigate = useNavigate();
    const session = useSession();
    const { getArticles, removeArticle } = Articles(session);
    
    const [articles, setArticles] = useState([]);
    const [totalCount, setTotalCount] = useState(0);
    const [showAdd, setShowAdd] = useState(false);
    const [searchTitle, setSearchTitle] = useState('');
    const [categoryFilters, setCategoryFilters] = useState(0);
    const [categoryFiltersList, setCategoryFiltersList] = useState([
        { id: 0, name: 'All Categories' },
        { id: 1, name: 'Technology' },
        { id: 2, name: 'Science' },
        { id: 3, name: 'Finance' },
        { id: 4, name: 'Sports' },
        { id: 5, name: 'News' }
    ]);
    const [sort, setSort] = useState('Published DESC');
    const [deleteModal, setDeleteModal] = useState(null);

    useEffect(() => {
        // Fetch articles from API when component mounts
        fetchArticles();
    }, []);

    useEffect(() => {
        filterArticles();
    }, [searchTitle, categoryFilters, sort]);

    const fetchArticles = () => {
        // Call the API with the proper ArticleListRequestModel
        getArticles({
            search: searchTitle || '',
            subjectIds: categoryFilters !== 0 ? [categoryFilters] : [],
            orderBy: getOrderByValue(sort),
            start: 1,
            length: 50,
            isActive: 0 // ArticleIsActive.Both
        }).then(response => {
            if (response.data.success) {
                setArticles(response.data.data.articles || []);
                setTotalCount(response.data.data.totalCount || 0);
            }
        }).catch(error => {
            console.error('Error fetching articles:', error);
        });
    };
    
    const getOrderByValue = (sortString) => {
        // Convert sort string to ArticleSortBy enum value
        const [field, direction] = sortString.split(' ');
        switch (field.toLowerCase()) {
            case 'title': return direction === 'ASC' ? 5 : 6; // Title ASC/DESC
            case 'domain': return direction === 'ASC' ? 7 : 8; // Domain ASC/DESC
            case 'published': return direction === 'ASC' ? 3 : 4; // Date ASC/DESC
            case 'views': return direction === 'ASC' ? 9 : 10; // Views ASC/DESC
            default: return 0; // BestScore
        }
    };
    
    const filterArticles = () => {
        fetchArticles();
    };

    const handleDelete = (article) => {
        setDeleteModal(article);
    };

    const handleDeleteClose = () => {
        setDeleteModal(null);
    };

    const handleDeleteConfirmed = (articleId) => {
        // Call the API to delete the article
        removeArticle(articleId).then(response => {
            if (response.data.success) {
                setArticles(articles.filter(article => article.id !== articleId));
                messages.success('Article deleted successfully');
            } else {
                messages.error('Failed to delete article');
            }
            handleDeleteClose();
        }).catch(error => {
            console.error('Error deleting article:', error);
            messages.error('An error occurred while deleting the article');
            handleDeleteClose();
        });
    };

    const DeleteModal = () => {
        return (<>
            <Modal
                title="Delete Article"
                onClose={handleDeleteClose}
            >
                <p>
                    Do you really want to delete the article "{deleteModal.title}"?
                    <br />
                    This will permanently remove the article from the system.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleDeleteConfirmed(deleteModal.id) }}>Yes</button>
                    <button className="cancel" onClick={handleDeleteClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const handleClosedAddArticle = (article) => {
        if(article) {
            // Refresh the article list to include the newly added article
            fetchArticles();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Article</button>
    </>);

    return (
        <div className="admin-articles">
            {showAdd && <div className="modal-placeholder">Add Article Modal would appear here</div>}
            {deleteModal != null && <DeleteModal></DeleteModal>}
            <Container
                title="Article Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="articlesearch"
                        type="text"
                        placeholder="Search by Title"
                        value={searchTitle}
                        onInput={(e) => setSearchTitle(e.target.value)}
                        className="titleInput"
                    />
                    <Select
                        options={categoryFiltersList.map(category => ({ value: category.id, label: category.name }))}
                        value={categoryFilters}
                        onChange={(e) => setCategoryFilters(e.target.value)}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('Title', sort))}>
                                Title {getSortIcon('Title', sort) && <span className="material-symbols-rounded">{getSortIcon('Title', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Domain', sort))}>
                                Domain {getSortIcon('Domain', sort) && <span className="material-symbols-rounded">{getSortIcon('Domain', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Published', sort))}>
                                Published {getSortIcon('Published', sort) && <span className="material-symbols-rounded">{getSortIcon('Published', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Status', sort))}>
                                Status {getSortIcon('Status', sort) && <span className="material-symbols-rounded">{getSortIcon('Status', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Views', sort))}>
                                Views {getSortIcon('Views', sort) && <span className="material-symbols-rounded">{getSortIcon('Views', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Category', sort))}>
                                Category {getSortIcon('Category', sort) && <span className="material-symbols-rounded">{getSortIcon('Category', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {articles.map(article =>
                            <tr 
                                key={article.id} 
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from action buttons
                                    if (e.target.closest('a')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    navigate('/admin/articles/edit/' + article.id);
                                }}
                            >
                                <td>{article.title}</td>
                                <td>{article.domain}</td>
                                <td>{article.published ? printDate(localDateTime(new Date(article.published))) : 'N/A'}</td>
                                <td>{article.status}</td>
                                <td>{article.views}</td>
                                <td>{article.category}</td>
                                <td className="buttons">
                                    <Link to={'/admin/articles/edit/' + article.id} title="edit article"><Icon name="edit_square"></Icon></Link>
                                    <Link 
                                        onClick={(e) => { 
                                            e.preventDefault(); 
                                            handleDelete(article); 
                                        }} 
                                        title="delete article"
                                    >
                                        <Icon name="delete"></Icon>
                                    </Link>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
