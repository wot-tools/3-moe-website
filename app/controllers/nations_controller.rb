class NationsController < ApplicationController

	def index
		@q = Nation.distinct.ransack(params[:q])
		@nations = @q.result.paginate(:page => params[:page], :per_page => 20)
	end
	
	def show
		@nation = Nation.find(params[:id])
		
		@q = @nation.tanks.ransack(params[:q])
		@tanks = @q.result.paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to nations_path and return
	end

end
