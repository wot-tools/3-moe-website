Rails.application.routes.draw do
  get 'welcome/index'
   
  resources :tanks, :only => [:index, :show]
  resources :players, :only => [:index, :show]
  resources :clans, :only => [:index, :show]
  resources :marks, :only => [:index, :show]

  root 'welcome#index'
  # For details on the DSL available within this file, see http://guides.rubyonrails.org/routing.html
end
